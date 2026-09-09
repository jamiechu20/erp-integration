# Spec — ERP 採購單同步與異常監控系統

## 1. Background

企業導入 SAP 後，MIS 部門常需要開發「外圍系統」處理 SAP 標準功能沒涵蓋、或需要跨系統整合的
自動化需求（逾期單據跟催、價格異常覆核、主檔同步等）。本專案模擬其中一種典型情境：
**採購單（Purchase Order）同步與異常監控**。

因 SAP 官方 sandbox 長期故障（見 [CONTEXT.md](../CONTEXT.md) 調查記錄），改用自建 Mock API
模擬 SAP `API_PURCHASEORDER_PROCESS_SRV` 的資料結構與行為。

## 2. Goals

- 定期從 SAP（Mock API）同步採購單資料到本地 DB
- 依據業務規則自動偵測「逾期未交貨」與「價格異常」兩類情況
- 異常發生時發送 Email 通知、並可在前端頁面查看
- 展示分層架構（Controller/Service/Repository）與排程整合能力

## 3. Non-Goals（明確排除）

- 不做三方比對（PO/GR/Invoice matching）——資料量與複雜度超出時程
- 不做簽核流程/多層審批
- 不做規則引擎（規則寫死在 Service 層，只有 2 條）
- 不處理 SAP 寫入（Create/Update PO），僅做唯讀同步
- 不追求測試覆蓋率，只覆蓋異常偵測核心邏輯

## 4. 業務情境

採購人員在 SAP 建立採購單後，MIS 系統應該要能：
1. 主動發現「已過交期但還沒完成收貨」的採購單，避免生產線因缺料停工才發現
2. 主動發現「單價明顯偏離歷史行情」的採購單，避免議價失誤或供應商異常報價未被及時發現

這兩件事在真實企業裡通常是採購/MIS人員手動用 Excel 拉報表比對，或依賴 SAP 標準報表
（但標準報表不會主動通知），本專案用自動化排程 + 通知取代人工盯盤。

## 5. 技術棧

C# / .NET（ASP.NET Core Web API）、EF Core（資料存取）、Quartz.NET（排程）、
MailKit（Email 通知）、Serilog（結構化 log）、xUnit（測試）、SQL 資料庫（Docker 本機開發）、
前端純 HTML + JavaScript（無前端框架）。

## 6. Mock API（`MockSap.Api` 專案）

模擬 SAP `API_PURCHASEORDER_PROCESS_SRV`（OData V4）的 Purchase Order Header + Item
結構，欄位名稱沿用 Day1 實測 SAP sandbox 時拿到的真實技術欄位名稱。

### Endpoint

`GET /PurchaseOrder?$expand=to_Item`

Response 範例：

```json
{
  "value": [
    {
      "PurchaseOrder": "4500000010",
      "PurchaseOrderType": "NB",
      "CompanyCode": "1000",
      "PurchasingOrganization": "US01",
      "PurchasingGroup": "A10",
      "Supplier": "2000000010",
      "PurchaseOrderDate": "2026-08-01",
      "DocumentCurrency": "USD",
      "PurchasingCompletenessStatus": false,
      "to_Item": [
        {
          "PurchaseOrder": "4500000010",
          "PurchaseOrderItem": "00010",
          "Material": "MAT-1001",
          "Plant": "PL01",
          "OrderQuantity": 100,
          "PurchaseOrderQuantityUnit": "EA",
          "NetPriceAmount": 25.5,
          "NetAmount": 2550,
          "DeliveryDate": "2026-08-20"
        }
      ]
    }
  ]
}
```

`PurchasingCompletenessStatus`、各欄位命名皆為 Day1 實測 SAP 官方 sandbox 回應時記錄下來的
真實欄位（詳見 CONTEXT.md），確保 Mock 資料結構跟真實 API 一致，正式環境只需替換 base URL。

`POST /PurchaseOrder` — demo 用，模擬「SAP 那邊新建了一張採購單」，讓下次同步能展示偵測到新資料。

**驗收**：`GET /PurchaseOrder?$expand=to_Item` 回傳的 JSON 結構與欄位名稱，需與本節範例一致；
`POST /PurchaseOrder` 新建的採購單，下次 `GET` 需能查到。

### 種子資料設計（啟動時載入，記憶體儲存）

6 筆採購單，其中刻意安排：
- 1 筆品項 `DeliveryDate` 為過去日期、Header `PurchasingCompletenessStatus = false` →
  第一次同步就會觸發**逾期異常**
- 1 筆品項 `NetPriceAmount` 明顯高於同物料其他品項（例如同 Material 其他單價都 ~25，
  這筆是 60）→ 第一次同步就會觸發**價格異常**
- 其餘 4 筆為正常資料，作為對照組

## 7. 本地 DB（EF Core）

| Table | 關鍵欄位 |
|---|---|
| PurchaseOrders | PurchaseOrder (PK), CompanyCode, Supplier, PurchasingCompletenessStatus, LastSyncedAt |
| PurchaseOrderItems | PurchaseOrder+PurchaseOrderItem (PK), Material, OrderQuantity, NetPriceAmount, NetAmount, DeliveryDate |
| SyncLogs | Id, RunAt, RecordsFetched, RecordsNew, AnomaliesFound |
| Anomalies | Id, PurchaseOrder, PurchaseOrderItem, RuleType（Overdue/PriceVariance）, Detail, DetectedAt, NotifiedAt |

**驗收**：EF Core Migration 執行後，四張表結構需與上表一致；`PurchaseOrders`/`PurchaseOrderItems`
的複合主鍵需能防止同一採購單品項被重複寫入。

## 8. 同步與異常偵測邏輯

1. Quartz.NET job 定期（demo 用 1 分鐘一次）呼叫 Mock API 拉全部採購單
2. 對每個新同步進來的品項（本地 DB 沒有的），寫入 DB，並執行下列規則：
   - **規則一：逾期未交貨** — `DeliveryDate < 今天` 且該筆所屬 Header 的
     `PurchasingCompletenessStatus == false` → 建立 `RuleType = Overdue` 的 Anomaly
   - **規則二：價格異常** — 計算本地 DB 中相同 `Material` 已存在品項的
     `NetPriceAmount` 平均值，若新品項單價偏離平均值超過 **30%** → 建立
     `RuleType = PriceVariance` 的 Anomaly（若該物料是第一次出現，無平均值可比，不判斷）
3. 每次同步結束寫入一筆 `SyncLog`

已存在的品項（非新資料）目前不重新檢查規則——真實情境中價格/交期異常通常在單據**新建時**
就該被發現，不需要對舊資料重複掃描，這也避免了「同一筆異常重複發通知」的問題。

**驗收**：種子資料中預先安排的 1 筆逾期品項、1 筆價格異常品項，第一次同步後 `Anomalies`
表應分別新增對應 `RuleType=Overdue`、`RuleType=PriceVariance` 各 1 筆；其餘 4 筆正常品項
不應觸發任何 Anomaly；同一品項重複同步不應重複寫入。

## 9. 通知設計與 Log

### 9.1 Email 通知

- 每筆 `NotifiedAt` 尚未寫入的 Anomaly，透過 MailKit 寄一封 Email（開發階段用 Mailpit/Papercut
  等本機 SMTP 測試工具，不寄真實信箱）
- 寄送時機：同步流程把新異常寫入 DB（`SaveChanges`）之後，再撈出所有 `NotifiedAt IS NULL`
  的異常逐筆寄送。用「未通知」而不是「本次新增」當寄送對象，是為了讓上次寄失敗的異常
  在下次同步自動補寄，不需要另外做 retry 機制
- Email 內容：異常類型、採購單號、品項編號、觸發原因（例如「單價 60 元，同物料歷史均價
  25 元，偏差 140%」）、偵測時間
- 寄送成功後更新該 Anomaly 的 `NotifiedAt`；寄送失敗只記 log，不讓同步流程失敗
  （同步是主要職責，通知是附加行為），`NotifiedAt` 維持 null 等下次同步補寄
- SMTP 連線資訊放在設定檔 `Smtp` 節點：`Host` / `Port` / `From` / `To` / `Enabled`，
  `Enabled=false` 時整個通知流程跳過（測試或無 SMTP 環境用）

**驗收**：本機 SMTP 測試工具需能收到與 Anomaly 內容相符的通知信；寄送成功後對應 Anomaly 的
`NotifiedAt` 需被寫入；SMTP 關閉或連不上時，同步本身仍需正常完成並回傳結果。

### 9.2 Serilog

- `ErpSync.Api` 以 Serilog 取代預設 logging，同時輸出到 Console 與檔案
  （`logs/erpsync-.log`，逐日 rolling）
- 關鍵流程需留下結構化 log：同步開始/結束（含抓取筆數、新增筆數、異常筆數）、
  偵測到異常、Email 寄送成功/失敗、排程 job 觸發與例外

## 10. ErpSync.Api 端點

| Method | Path | 說明 |
|---|---|---|
| GET | `/api/purchase-orders` | 查詢已同步採購單，可用 `supplier`、`companyCode` 篩選 |
| GET | `/api/purchase-orders/{id}/items` | 查詢單一採購單的品項 |
| GET | `/api/anomalies` | 查詢異常清單，可用 `ruleType` 篩選 |
| GET | `/api/sync-logs` | 查詢同步歷史 |
| POST | `/api/sync/run` | 手動觸發一次同步（demo 用） |

**驗收**：每個端點需能用 Postman/curl 實際呼叫成功，篩選參數（`supplier`、`companyCode`、
`ruleType`）需能正確過濾結果。

## 11. 前端頁面（`ErpSync.Api/wwwroot`）

純 HTML + JavaScript（無框架），由 `ErpSync.Api` 以靜態檔案直接提供，同源呼叫 §10 的查詢 API。

- 三個分頁：**採購單**、**異常清單**、**同步紀錄**
- 採購單頁：supplier / companyCode 篩選欄位；點一列展開該採購單的品項（呼叫
  `/api/purchase-orders/{id}/items`）
- 異常清單頁：ruleType 下拉篩選（全部 / Overdue / PriceVariance），顯示是否已通知（`NotifiedAt`）
- 同步紀錄頁：列出每次同步的時間與筆數統計
- 右上角「立即同步」按鈕呼叫 `POST /api/sync/run`，完成後重新載入當前分頁資料

**驗收**：頁面在瀏覽器打開後，三個分頁都能顯示 DB 內的實際資料；篩選欄位變更後清單需正確
過濾；按下「立即同步」後同步紀錄頁需多一筆記錄。

## 12. 測試策略（`tests/ErpSync.Application.Tests`）

測試框架用 xUnit，範圍只覆蓋 **Application 層的核心邏輯**（`PurchaseOrderSyncService`），
不對 Controller、EF Core Repository、MailKit 寄信做整合測試——Repository 與寄信在開發過程
已用本機 SQL Server 與 Mailpit 端到端驗證過，單元測試的價值集中在「規則判斷正確與否」。

- 外部相依（`ISapPurchaseOrderClient` / 各 Repository / `IUnitOfWork` / `IAnomalyNotifier`）
  一律用手寫的 in-memory Fake，不引入 mock 框架：Fake 直接持有 List/Dictionary，
  斷言時讀 Fake 的內容即可，讀起來比 mock 的 Setup/Verify 更接近「同步跑完 DB 長什麼樣」
- 測項對應 §8 的驗收條件：

| # | 測項 | 期望 |
|---|---|---|
| 1 | 逾期且未完成收貨的品項 | 產生 1 筆 `Overdue` |
| 2 | 逾期但 `PurchasingCompletenessStatus == true` | 不產生 Anomaly |
| 3 | 交期未到且未完成收貨 | 不產生 Anomaly |
| 4 | 物料第一次出現（無歷史均價） | 不判斷價格規則，不產生 Anomaly |
| 5 | 單價偏離歷史均價超過 30%（偏高、偏低各一） | 產生 1 筆 `PriceVariance`，`Detail` 含偏差百分比 |
| 6 | 單價偏離未超過 30%（含剛好 30% 的邊界） | 不產生 Anomaly |
| 7 | 同一批同步中，同物料第二筆與第一筆比價 | 均價在同批資料內逐筆累加後生效 |
| 8 | 本地 DB 已存在的品項再次同步 | 不重複寫入、不重複產生 Anomaly |
| 9 | 一次同步結束 | 寫入 1 筆 `SyncLog`，三個計數欄位與實際結果一致 |
| 10 | 通知（§9.1） | 寄送成功回寫 `NotifiedAt`；寄送失敗時同步仍成功、`NotifiedAt` 維持 null |

**驗收**：`dotnet test` 全數通過；任一條規則的門檻或條件被改動時，對應測項需失敗。

## 13. 部署

### 13.1 本機 Docker Compose

`docker-compose.yml` 一次帶起 demo 需要的四個容器，讓不熟悉專案的人不必手動裝 SQL Server
與 SMTP 工具：

| 服務 | 說明 | 對外埠 |
|---|---|---|
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest`，本地 DB | 1433 |
| `mailpit` | 本機 SMTP 測試工具，收異常通知信 | 1025 (SMTP) / 8025 (Web UI) |
| `mocksap` | `MockSap.Api`，模擬 SAP OData API | 5173 |
| `erpsync` | `ErpSync.Api`（含 Quartz 排程與前端頁面） | 8080 |

- 兩個 API 各自一份多階段 build 的 `Dockerfile`（`sdk` 階段 publish、`aspnet` 階段執行）
- 連線字串、`SapApi:BaseUrl`、`Smtp:Host` 以環境變數覆寫 `appsettings.json`，
  程式碼不需要為容器環境做任何分支
- 容器內不做 HTTPS 轉址（沒有憑證），以設定 `EnableHttpsRedirection`（預設 true）控制，
  compose 內設為 false
- DB schema 由 `ErpSync.Api` 啟動時自動套用 Migration（設定 `Database:AutoMigrate=true` 才執行），
  避免容器環境還要另外跑 `dotnet ef database update`；SQL Server 容器啟動較慢，
  套用前以重試等待其就緒

**驗收**：在乾淨環境執行 `docker compose up -d --build` 後，`http://localhost:8080` 能開啟
前端頁面、排程在 1 分鐘內完成第一次同步、異常清單出現 2 筆、Mailpit UI 收到對應通知信。

### 13.2 Azure App Service

以「展示得起來」為目標，不做 CI/CD pipeline：

- `MockSap.Api` 與 `ErpSync.Api` 各部署為一個 Linux App Service（同一個 App Service Plan）
- DB 用 Azure SQL Database（Basic 層級），連線字串放 App Service 的
  Connection strings（`Default`，type = SQLAzure），不進 git
- `SapApi__BaseUrl` 指向 Mock API 的 App Service URL；雲端環境沒有 SMTP，
  `Smtp__Enabled=false` 關閉通知（同步流程本來就設計成通知失敗不影響同步）
- 部署方式：`dotnet publish` 後用 `az webapp deploy` 上傳 zip，步驟寫在
  `docs/deploy-azure.md`，讓流程可重現

**驗收**：Azure 上的 ErpSync App Service URL 可直接開啟前端頁面並看到同步後的資料。

## 14. Out of Scope（Non-Goals 的具體對應）

- 前端只做清單呈現與篩選，不做圖表視覺化
- 不處理多幣別金額換算比較（NetAmount 比較僅限同幣別內的簡單場景）
- 不處理採購單狀態機轉換（例如核准/駁回流程）

## 15. Demo Script

1. 啟動 Mock API + ErpSync.Api，觸發第一次同步
2. 前端「異常清單」直接顯示 2 筆異常（1 筆逾期、1 筆價格異常）——不需要臨時操作，種子資料已內建
3. 用 Postman 對 Mock API `POST /PurchaseOrder` 建一筆新的、故意設計成正常的採購單
4. 手動觸發同步，前端「採購單列表」多一筆資料，異常清單不變（因為是正常資料）
5. 講解 Email 通知怎麼觸發、SyncLog 怎麼查
