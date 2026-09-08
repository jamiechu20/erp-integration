using ErpSync.Application.Interfaces;
using ErpSync.Application.Mapping;
using ErpSync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ErpSync.Application.Services;

/// <summary>
/// 打 Mock SAP API 拉採購單，把本地 DB 沒有的品項寫入，並依 spec.md §8 規則偵測異常。
/// </summary>
public class PurchaseOrderSyncService : IPurchaseOrderSyncService
{
    // 價格偏離歷史均價超過 30% 視為異常（spec.md §8 規則二）
    private const decimal PriceVarianceThreshold = 0.3m;

    private readonly ISapPurchaseOrderClient _sapClient;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IAnomalyRepository _anomalyRepository;
    private readonly ISyncLogRepository _syncLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAnomalyNotifier _notifier;
    private readonly ILogger<PurchaseOrderSyncService> _logger;

    public PurchaseOrderSyncService(
        ISapPurchaseOrderClient sapClient,
        IPurchaseOrderRepository purchaseOrderRepository,
        IAnomalyRepository anomalyRepository,
        ISyncLogRepository syncLogRepository,
        IUnitOfWork unitOfWork,
        IAnomalyNotifier notifier,
        ILogger<PurchaseOrderSyncService> logger)
    {
        _sapClient = sapClient;
        _purchaseOrderRepository = purchaseOrderRepository;
        _anomalyRepository = anomalyRepository;
        _syncLogRepository = syncLogRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("開始同步採購單");

        var sapOrders = await _sapClient.GetPurchaseOrdersAsync(cancellationToken);
        var existingItemKeys = await _purchaseOrderRepository.GetExistingItemKeysAsync(cancellationToken);
        var trackedHeaders = await _purchaseOrderRepository.GetTrackedByIdsAsync(
            sapOrders.Select(o => o.PurchaseOrder), cancellationToken);
        var materialPriceStats = await _purchaseOrderRepository.GetMaterialPriceStatsAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var recordsFetched = sapOrders.Sum(o => o.Items.Count);
        var recordsNew = 0;
        var anomaliesFound = 0;

        foreach (var sapOrder in sapOrders)
        {
            var newItemDtos = sapOrder.Items
                .Where(i => !existingItemKeys.Contains((sapOrder.PurchaseOrder, i.PurchaseOrderItem)))
                .ToList();

            if (newItemDtos.Count == 0)
            {
                continue;
            }

            if (!trackedHeaders.TryGetValue(sapOrder.PurchaseOrder, out var header))
            {
                header = PurchaseOrderMapper.ToEntity(sapOrder, now);
                header.Items = new List<PurchaseOrderItem>();
                await _purchaseOrderRepository.AddAsync(header, cancellationToken);
            }
            else
            {
                header.LastSyncedAt = now;
            }

            foreach (var itemDto in newItemDtos)
            {
                var item = PurchaseOrderMapper.ToEntity(itemDto);
                header.Items.Add(item);

                anomaliesFound += await DetectAnomaliesAsync(
                    sapOrder.PurchasingCompletenessStatus, item, materialPriceStats, today, now, cancellationToken);
            }

            recordsNew += newItemDtos.Count;
        }

        var syncLog = new SyncLog
        {
            RunAt = now,
            RecordsFetched = recordsFetched,
            RecordsNew = recordsNew,
            AnomaliesFound = anomaliesFound,
        };
        await _syncLogRepository.AddAsync(syncLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 先存檔再寄信：Anomaly 拿到 Id 之後才有東西可以回寫 NotifiedAt（spec.md §9.1）
        await NotifyPendingAnomaliesAsync(cancellationToken);

        _logger.LogInformation(
            "同步完成：抓取 {RecordsFetched} 筆、新增 {RecordsNew} 筆、異常 {AnomaliesFound} 筆",
            recordsFetched, recordsNew, anomaliesFound);

        return new SyncResult(recordsFetched, recordsNew, syncLog.AnomaliesFound);
    }

    /// <summary>
    /// 只對「新同步進來」的品項執行規則檢查（spec.md §8）。materialPriceStats 在同一次同步中
    /// 逐筆累加，確保同一批資料裡後面的品項能拿前面品項的單價當歷史均價比較基準。
    /// </summary>
    private async Task<int> DetectAnomaliesAsync(
        bool purchasingCompletenessStatus,
        PurchaseOrderItem item,
        Dictionary<string, (decimal Sum, int Count)> materialPriceStats,
        DateOnly today,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var anomalyCount = 0;

        if (item.DeliveryDate < today && !purchasingCompletenessStatus)
        {
            await _anomalyRepository.AddAsync(
                new Anomaly
                {
                    PurchaseOrder = item.PurchaseOrder,
                    PurchaseOrderItemNumber = item.PurchaseOrderItemNumber,
                    RuleType = AnomalyRuleType.Overdue,
                    Detail = $"交期 {item.DeliveryDate:yyyy-MM-dd} 已過，尚未完成收貨",
                    DetectedAt = now,
                },
                cancellationToken);
            anomalyCount++;
            _logger.LogInformation("偵測到逾期異常：PO {PurchaseOrder}-{ItemNumber}，交期 {DeliveryDate}",
                item.PurchaseOrder, item.PurchaseOrderItemNumber, item.DeliveryDate);
        }

        materialPriceStats.TryGetValue(item.Material, out var stats);
        if (stats.Count > 0)
        {
            var averagePrice = stats.Sum / stats.Count;
            var deviation = Math.Abs(item.NetPriceAmount - averagePrice) / averagePrice;

            if (deviation > PriceVarianceThreshold)
            {
                await _anomalyRepository.AddAsync(
                    new Anomaly
                    {
                        PurchaseOrder = item.PurchaseOrder,
                        PurchaseOrderItemNumber = item.PurchaseOrderItemNumber,
                        RuleType = AnomalyRuleType.PriceVariance,
                        Detail = $"單價 {item.NetPriceAmount} 元，同物料歷史均價 {averagePrice:0.##} 元，偏差 {deviation:P0}",
                        DetectedAt = now,
                    },
                    cancellationToken);
                anomalyCount++;
                _logger.LogInformation(
                    "偵測到價格異常：PO {PurchaseOrder}-{ItemNumber}，單價 {NetPrice}、均價 {AveragePrice}、偏差 {Deviation:P0}",
                    item.PurchaseOrder, item.PurchaseOrderItemNumber, item.NetPriceAmount, averagePrice, deviation);
            }
        }

        materialPriceStats[item.Material] = (stats.Sum + item.NetPriceAmount, stats.Count + 1);

        return anomalyCount;
    }

    /// <summary>
    /// 撈出所有還沒通知的異常逐筆寄送，成功才寫 NotifiedAt（spec.md §9.1）。
    /// 用「未通知」而不是「本次新增」當寄送對象，上次寄失敗的下次同步會自動補寄。
    /// </summary>
    private async Task NotifyPendingAnomaliesAsync(CancellationToken cancellationToken)
    {
        var pending = await _anomalyRepository.GetUnnotifiedAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        var notifiedCount = 0;
        foreach (var anomaly in pending)
        {
            if (await _notifier.NotifyAsync(anomaly, cancellationToken))
            {
                anomaly.NotifiedAt = DateTime.UtcNow;
                notifiedCount++;
            }
        }

        if (notifiedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("異常通知：待通知 {PendingCount} 筆，成功寄出 {NotifiedCount} 筆",
            pending.Count, notifiedCount);
    }
}
