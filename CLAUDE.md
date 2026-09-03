# 協作規則

本文件只描述這個專案的開發方式（模擬真實工作中從 0 到 1、透過 AI agent 開發程式的流程）。
專案背景與動機交代在 [CONTEXT.md](CONTEXT.md)（不進 git），每日任務進度見
[progress/plan.md](progress/plan.md)（同樣不進 git）。

## 溝通

- 使用繁體中文溝通，技術術語保留英文
- 回答要直接說重點，不需要過度鼓勵、不要浮誇的稱讚
- 時間估算要貼近現實，不要過度樂觀
- 任務安排以能否在時程內做出完整可展示的作品為優先，不追求功能的完整性
- 每次進度都應該產出可見成果（程式碼、README、可執行的功能等），不要卡在「觀念讀完才開始寫」

## Agentic coding 開發方式

1. **先寫 spec，再生成程式碼** — 新功能先在 `docs/spec.md` 補上資料模型/端點/規則，
   不要用模糊 prompt 一次生成整包
   - `docs/spec.md` 會進 git、公開在 repo 裡，內容要保持**一體適用**：只寫技術/業務邏輯本身，
     不要寫任何求職脈絡或應徵對象相關的字句（那些資訊留在不進 git 的 `CONTEXT.md`）
   - spec 採用業界常見的技術規格文件結構：Background、Goals、**Non-Goals**（明確排除範圍）、
     業務情境、資料模型、API 設計（含 Request/Response 範例，不只列端點名稱）、Out of Scope
2. **小顆粒 commit** — 每個 commit 對應一個明確功能或修正，commit message 清楚對應
   spec 的段落，讓 git history 本身能反映真實的迭代開發過程
3. **README 記錄開發流程** — 用了什麼 prompt、AI 產出後做了哪些架構調整、抓出哪些 bug、
   哪些技術決策是自己主導的（分層設計、例外處理策略、要不要加快取層等）
4. **不留看不懂的程式碼** — 每段 AI 產出的邏輯都要能自己解釋「這行在幹嘛、為什麼這樣寫」，
   如果生成的邏輯看不懂，要在對話中提出來討論，不要直接接受

## 技術棧

**C# / .NET 10**（SDK 10.0.400）為主要開發語言與框架，所有後端專案（含 Mock API）皆用
ASP.NET Core 開發，資料存取用 EF Core，排程用 Quartz.NET，Email 用 MailKit，Log 用 Serilog，
測試用 xUnit。前端純 HTML + JS，不使用前端框架。

## 架構

同步（寫入）與查詢（讀取）是兩條各自獨立的路徑，只在 DB 交會，不是一條線性管線：

- **同步路徑**：`SyncWorker (Quartz.NET 排程) 定期呼叫 Mock SAP OData API → 寫入 SQL DB (Docker)`
- **查詢路徑**：`前端 (純 HTML+JS) → ASP.NET Core Web API (Controller/Service/Repository) → 讀取 SQL DB`

`SyncWorker` 是 ErpSync.Api 這個 ASP.NET Core process 裡的一個背景服務（Hosted Service），
跟 Controller 共用同一套 Repository 層存取 DB，不是分開部署的服務，也不會經過 Web API 的
HTTP 層去寫資料。

原計畫是直接打 SAP Business Accelerator Hub 的真實 sandbox，但實測後確認該 sandbox
後端閘道長期故障（見 [CONTEXT.md](CONTEXT.md) 的調查記錄），因此改為自建 mock API，
欄位結構與 OData 查詢語法比照真實 SAP API，正式環境只需替換 base URL。

## 範圍界線（避免功能蔓延）

- 異常偵測規則：1-2 條即可，不做規則引擎
- 測試：只覆蓋 Service 層核心邏輯，不追求覆蓋率
- Azure 部署：能跑起來、有 URL 可展示即可，不做 CI/CD pipeline
- 前端：功能性 HTML+JS，不做 UI 美化
