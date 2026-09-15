# ERP 採購單同步與異常監控系統

模擬企業 MIS 常見的「ERP 外圍系統」工作型態：定期從 SAP（此專案為自建 Mock API）同步採購單資料，
自動偵測逾期未交貨、價格異常兩類情況，異常發生時寄送 Email 通知，並提供前端頁面查看結果。

完整規格見 [docs/spec.md](docs/spec.md)。

## 架構

同步（寫入）與查詢（讀取）是兩條各自獨立的路徑，只在 DB 交會：

```
同步路徑：SyncWorker (Quartz.NET 排程，ErpSync.Api 內的背景服務)
              → 定期呼叫 MockSap.Api (OData 風格 API)
              → 寫入 SQL Server

查詢路徑：前端 (純 HTML+JS)
              → ErpSync.Api (Controller/Service/Repository)
              → 讀取 SQL Server
```

`SyncWorker` 跟 Controller 共用同一套 Repository 層存取 DB，不是分開部署的服務。

原計畫是直接打 SAP Business Accelerator Hub 的真實 sandbox，但實測後確認該 sandbox
後端閘道長期故障（見下方〈SAP Sandbox 排查記錄〉），因此改為自建 Mock API，
欄位結構與 OData 查詢語法比照真實 SAP API，正式環境只需替換 base URL。

## 技術棧

C# / .NET 10（SDK 10.0.400），ASP.NET Core Web API，EF Core，Quartz.NET（排程），
MailKit（Email 通知），Serilog（結構化 log），xUnit（測試），SQL Server（Docker），
前端純 HTML + JavaScript（無框架）。

## 專案結構

```
src/
  ErpSync.Api/            Web API + Quartz 排程 (SyncWorker) + 前端靜態檔案 (wwwroot)
  ErpSync.Application/    Service 層、DTO、Repository 介面
  ErpSync.Infrastructure/ EF Core、SAP Client、Email 通知實作
  ErpSync.Domain/         Entity
  MockSap.Api/            模擬 SAP OData API，欄位結構比照真實 API
tests/
  ErpSync.Application.Tests/  xUnit，覆蓋 PurchaseOrderSyncService 核心邏輯
docs/
  spec.md                 技術規格（資料模型、API、業務規則）
  deploy-azure.md          Azure App Service 部署步驟
```

## 怎麼跑起來

### Docker Compose（推薦，一次帶起整套環境）

```bash
docker compose up -d --build
```

| 服務 | 說明 | 網址 |
|---|---|---|
| 前端頁面 | ErpSync.Api 提供的靜態頁面 | http://localhost:8080 |
| ErpSync.Api 文件 | Scalar API 文件 | http://localhost:8080/scalar |
| MockSap.Api 文件 | Scalar API 文件 | http://localhost:5173/scalar |
| Mailpit | 收異常通知信（本機 SMTP 測試工具） | http://localhost:8025 |

排程預設 1 分鐘跑一次，起來後最多等 1 分鐘，前端「異常清單」就會出現種子資料觸發的異常。

> **注意**：`sqlserver-data` 是具名 volume，`docker compose down` 不會清掉它。如果要看
> 「乾淨環境第一次同步」的效果（異常清單只有種子資料的 2 筆），要用
> `docker compose down -v` 把 volume 一併刪除再重新 `up`，不然舊資料會一直留著。

### 本機 dotnet run（開發用）

```bash
# 先起 SQL Server 和 Mailpit（沿用 compose 定義，只跑這兩個服務）
docker compose up -d sqlserver mailpit

cd src/MockSap.Api && dotnet run &
cd src/ErpSync.Api && dotnet run
```

`dotnet run` 會套用 `launchSettings.json` 的 `ASPNETCORE_ENVIRONMENT=Development`，
但這個專案的 API 文件端點不分環境都開放，本機跟容器行為一致。

### 測試

```bash
dotnet test
```

## Demo 流程

1. `docker compose up -d --build`，等排程自動觸發第一次同步
2. 前端「異常清單」直接顯示 2 筆異常（1 筆逾期、1 筆價格異常）——種子資料內建，不需要臨時操作
3. 用 Scalar（http://localhost:5173/scalar）對 MockSap.Api `POST /PurchaseOrder` 建一筆新的正常採購單
4. 前端右上角「立即同步」，「採購單列表」多一筆資料，異常清單不變
5. 開 Mailpit（http://localhost:8025）看異常通知信；「同步紀錄」分頁看每次同步的統計

## SAP Sandbox 排查記錄

原計畫直接串接 SAP Business Accelerator Hub（api.sap.com）的免費 sandbox，串接方式與真實
SAP 系統一致。實際申請帳號、拿到 API Key 後，依序測試 Business Partner、Purchase Order、
Sales Order 三支不同模組的 sandbox OData API，全部回傳 `401 Anmeldung fehlgeschlagen`
（SAP 後端登入失敗）。

排查過程：

1. 確認 URL 路徑與官方文件一致
2. 確認 API Key 的傳遞方式（HTTP header）正確——header 大小寫不敏感，換成 query param
   會被閘道層直接拒絕，代表閘道層有正確辨識到 header，問題出在閘道轉發到後端之後
3. 查看該 API 的 Authentication Methods，發現列出的是 Basic Authentication、
   OAuth 2.0 Authorization Code、X.509，**完全沒有 API Key** 這個選項——代表 API Key
   只是 API Business Hub 閘道層（Apigee）用來做額度控管的機制，閘道收到 Key 後應自動轉換成
   後端測試帳密做 Basic Auth 登入，觀察到的錯誤就是這層「Key 換帳密」的自動轉換機制故障
4. 也測試過另一個「AI-generated API sandbox」選項，回應看似正常，但用瀏覽器 DevTools
   檢查實際網路請求後發現它只會打到 SAP 網站自己的內部端點，並沒有呼叫任何外部 OData API，
   代表這只是網站內建的展示功能，無法讓外部程式呼叫
5. 搜尋確認 SAP 官方社群論壇上已有其他人回報同樣的 401 問題，時間點早於本次測試，
   代表這是長期性、系統性的故障，不是帳號、Key、或串接方式的問題，等待也不會自行恢復

**決策**：放棄依賴 SAP 真實 sandbox，改為自建 Mock SAP OData API（`MockSap.Api` 專案），
資料結構沿用實測拿到的真實欄位，回傳格式與查詢語法比照真實 SAP API，架構完全不變，
只是把資料來源從「打真實 SAP sandbox」換成「打自建的 mock 服務」。

## 開發流程

這個專案採用 agentic coding 的開發方式：先在 `docs/spec.md` 補規格（資料模型、API、
業務規則），再依 spec 生成程式碼，每個 commit 對應 spec 的一個段落。完整每日進度、
checklist 與過程中抓到的問題見 git commit history；重要技術決策摘要如下：

- **Mock API 取代真實 SAP sandbox**：見上方排查記錄，是本專案最大的架構調整，
  在 Day1 就發生，影響後續所有天數的實作方向
- **異常偵測規則的比較基準**：價格異常規則比較「本地 DB 已存在的同物料均價」，
  同一批同步中後面的品項要能拿到前面品項算出的均價，這點在寫測試時才發現需要逐筆
  累加而不是同步結束後一次計算，否則同一批資料裡的異常會被漏判
- **通知補寄機制**：Email 寄送失敗不影響同步流程本身，寄送對象抓「尚未通知」而不是
  「本次新增」，讓上次失敗的異常能在下次同步自動補寄，不需要另外寫 retry 邏輯
- **API 文件用 Scalar 而非 Swagger UI**：.NET 內建的 `Microsoft.AspNetCore.OpenApi`
  只產生 OpenAPI JSON spec，不含網頁介面；額外接 Scalar 提供可互動的文件頁面，
  且不分環境開放（demo 專案優先方便展示，取捨見對應 commit）

### 開發過程中抓到的 bug

- 前端沒設 `color-scheme`，瀏覽器深色模式下白底文字整片看不見 → 固定 light
- 「立即同步」後狀態列被 loader 的「N 筆」訊息蓋掉 → 改成先重載清單再寫同步結果
- Docker Compose 的 `sqlserver-data` 是具名 volume，`down` 不會清掉，重跑 demo 前
  如果沒注意會看到「異常 0 筆」（因為資料早就同步過），已在上方〈怎麼跑起來〉註明
  要用 `down -v` 才能重現乾淨環境的效果

## 已知限制（Out of Scope）

- 不做三方比對（PO/GR/Invoice matching）、簽核流程、規則引擎（規則寫死在 Service 層）
- 不處理 SAP 寫入（Create/Update PO），僅做唯讀同步
- 測試只覆蓋 Service 層核心邏輯，不追求覆蓋率
- 前端只做清單呈現與篩選，不做圖表視覺化、不處理多幣別換算、不處理狀態機轉換

完整範圍界線見 [docs/spec.md](docs/spec.md) 的 Non-Goals 與 Out of Scope 章節。

## 部署

Docker Compose 為本機/展示用途，已驗證可用。Azure App Service 部署步驟見
[docs/deploy-azure.md](docs/deploy-azure.md)（需要 Azure 訂閱與 `az login`，屬於一次性
手動操作，尚未在本機執行）。
