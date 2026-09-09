# 部署到 Azure App Service

對應 [spec.md](spec.md) §13.2。目標是「雲端有一個可以打開的 URL」，不做 CI/CD pipeline，
所有步驟都用 `az` CLI 手動執行，可重現。

## 0. 前置

```bash
brew install azure-cli     # macOS
az login
az account set --subscription "<你的訂閱名稱或 ID>"
```

以下變數在同一個 shell 內先設好，後面的指令直接引用：

```bash
RG=rg-erpsync
LOC=southeastasia
PLAN=plan-erpsync
APP_MOCK=mocksap-erpsync-demo      # 名稱要全域唯一，衝突就換一個後綴
APP_SYNC=erpsync-demo
SQL_SERVER=sql-erpsync-demo
SQL_DB=ErpSync
SQL_ADMIN=erpadmin
SQL_PASSWORD='<自己設一組強密碼，不要寫進 git>'
```

## 1. Resource Group 與 App Service Plan

```bash
az group create -n $RG -l $LOC
az appservice plan create -g $RG -n $PLAN --is-linux --sku B1
```

> B1 而不是免費的 F1：F1 不支援 Always On，App Service 閒置後會被回收，
> Quartz.NET 的背景排程就跟著停掉。要省錢可以先用 F1，demo 前先打一次 URL 把它叫醒。

## 2. Azure SQL Database

```bash
az sql server create -g $RG -n $SQL_SERVER -l $LOC \
  --admin-user $SQL_ADMIN --admin-password "$SQL_PASSWORD"

# 允許 Azure 內部服務（App Service）連線；0.0.0.0 是 Azure 專用的特殊規則，不是對外全開
az sql server firewall-rule create -g $RG -s $SQL_SERVER \
  -n AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

az sql db create -g $RG -s $SQL_SERVER -n $SQL_DB --service-objective Basic
```

DB schema 不需要另外跑 `dotnet ef database update` — `ErpSync.Api` 啟動時會依
`Database:AutoMigrate` 設定自動套用 Migration（spec.md §13.1）。

## 3. 建立兩個 Web App

```bash
az webapp list-runtimes --os linux | grep -i dotnet   # 先確認可用的 .NET 版本字串

az webapp create -g $RG -p $PLAN -n $APP_MOCK --runtime "DOTNETCORE:10.0"
az webapp create -g $RG -p $PLAN -n $APP_SYNC --runtime "DOTNETCORE:10.0"
```

> 若清單裡還沒有 .NET 10，改走容器路線：repo 內兩支 `Dockerfile` 直接可用，
> 推到 Azure Container Registry 後用 `az webapp create --deployment-container-image-name` 建立。

## 4. 設定 ErpSync.Api 的組態

連線字串放 Connection strings（不是一般 App settings），型別選 `SQLAzure`：

```bash
az webapp config connection-string set -g $RG -n $APP_SYNC \
  --connection-string-type SQLAzure --settings \
  Default="Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;"

az webapp config appsettings set -g $RG -n $APP_SYNC --settings \
  SapApi__BaseUrl="https://$APP_MOCK.azurewebsites.net" \
  Smtp__Enabled=false \
  Database__AutoMigrate=true \
  Sync__IntervalMinutes=5

az webapp config set -g $RG -n $APP_SYNC --always-on true
```

- 雲端沒有 SMTP 主機，`Smtp__Enabled=false` 直接關掉通知；同步流程本來就設計成
  通知失敗不影響同步（spec.md §9.1），所以關掉不會有副作用
- `Sync__IntervalMinutes=5`：雲端不需要像本機 demo 那樣 1 分鐘一次

## 5. Publish 並部署

```bash
dotnet publish src/MockSap.Api/MockSap.Api.csproj -c Release -o ./publish/mocksap
dotnet publish src/ErpSync.Api/ErpSync.Api.csproj -c Release -o ./publish/erpsync

(cd ./publish/mocksap && zip -r ../mocksap.zip .)
(cd ./publish/erpsync && zip -r ../erpsync.zip .)

az webapp deploy -g $RG -n $APP_MOCK --src-path ./publish/mocksap.zip --type zip
az webapp deploy -g $RG -n $APP_SYNC --src-path ./publish/erpsync.zip --type zip
```

## 6. 驗證

```bash
curl "https://$APP_MOCK.azurewebsites.net/PurchaseOrder?\$expand=to_Item" | head -c 300
curl "https://$APP_SYNC.azurewebsites.net/api/sync-logs"
open "https://$APP_SYNC.azurewebsites.net/"        # 前端頁面
az webapp log tail -g $RG -n $APP_SYNC             # 看 Serilog Console 輸出
```

驗收標準：前端頁面打得開，採購單列表有 6 筆種子資料、異常清單有 2 筆
（1 筆 Overdue、1 筆 PriceVariance）。

## 7. 收尾（demo 結束後停止計費）

```bash
az group delete -n $RG --yes --no-wait
```
