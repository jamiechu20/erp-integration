using ErpSync.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ErpSync.Infrastructure.Scheduling;

/// <summary>
/// SyncWorker：定期呼叫 Mock SAP API 同步採購單（spec.md §8）。
/// 是 ErpSync.Api process 內的背景服務，跟 Controller 共用同一套 Repository 層，
/// 不會經過 Web API 的 HTTP 層去寫資料（見 CLAUDE.md 架構說明）。
/// </summary>
[DisallowConcurrentExecution]
public class PurchaseOrderSyncJob : IJob
{
    private readonly IPurchaseOrderSyncService _syncService;
    private readonly ILogger<PurchaseOrderSyncJob> _logger;

    public PurchaseOrderSyncJob(IPurchaseOrderSyncService syncService, ILogger<PurchaseOrderSyncJob> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("排程觸發同步（trigger {TriggerKey}）", context.Trigger.Key);

        try
        {
            var result = await _syncService.SyncAsync(context.CancellationToken);
            _logger.LogInformation(
                "排程同步完成：Fetched={RecordsFetched} New={RecordsNew} Anomalies={AnomaliesFound}",
                result.RecordsFetched, result.RecordsNew, result.AnomaliesFound);
        }
        catch (Exception ex)
        {
            // 單次同步失敗（例如 Mock API 沒開）不該讓排程停掉，記 log 後等下一次觸發
            _logger.LogError(ex, "排程同步失敗，將於下次觸發重試");
        }
    }
}
