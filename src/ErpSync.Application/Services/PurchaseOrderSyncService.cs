using ErpSync.Application.Interfaces;
using ErpSync.Application.Mapping;
using ErpSync.Domain.Entities;

namespace ErpSync.Application.Services;

/// <summary>
/// 打 Mock SAP API 拉採購單，把本地 DB 沒有的品項寫入。
/// 異常偵測規則（逾期/價格異常，spec.md §8）在 Day4 加入，這裡先只負責同步與落地。
/// </summary>
public class PurchaseOrderSyncService : IPurchaseOrderSyncService
{
    private readonly ISapPurchaseOrderClient _sapClient;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ISyncLogRepository _syncLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseOrderSyncService(
        ISapPurchaseOrderClient sapClient,
        IPurchaseOrderRepository purchaseOrderRepository,
        ISyncLogRepository syncLogRepository,
        IUnitOfWork unitOfWork)
    {
        _sapClient = sapClient;
        _purchaseOrderRepository = purchaseOrderRepository;
        _syncLogRepository = syncLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var sapOrders = await _sapClient.GetPurchaseOrdersAsync(cancellationToken);
        var existingItemKeys = await _purchaseOrderRepository.GetExistingItemKeysAsync(cancellationToken);
        var trackedHeaders = await _purchaseOrderRepository.GetTrackedByIdsAsync(
            sapOrders.Select(o => o.PurchaseOrder), cancellationToken);

        var now = DateTime.UtcNow;
        var recordsFetched = sapOrders.Sum(o => o.Items.Count);
        var recordsNew = 0;

        foreach (var sapOrder in sapOrders)
        {
            var newItemDtos = sapOrder.Items
                .Where(i => !existingItemKeys.Contains((sapOrder.PurchaseOrder, i.PurchaseOrderItem)))
                .ToList();

            if (newItemDtos.Count == 0)
            {
                continue;
            }

            if (trackedHeaders.TryGetValue(sapOrder.PurchaseOrder, out var existingHeader))
            {
                existingHeader.LastSyncedAt = now;
                foreach (var itemDto in newItemDtos)
                {
                    existingHeader.Items.Add(PurchaseOrderMapper.ToEntity(itemDto));
                }
            }
            else
            {
                var header = PurchaseOrderMapper.ToEntity(sapOrder, now);
                await _purchaseOrderRepository.AddAsync(header, cancellationToken);
            }

            recordsNew += newItemDtos.Count;
        }

        var syncLog = new SyncLog
        {
            RunAt = now,
            RecordsFetched = recordsFetched,
            RecordsNew = recordsNew,
            AnomaliesFound = 0,
        };
        await _syncLogRepository.AddAsync(syncLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SyncResult(recordsFetched, recordsNew, syncLog.AnomaliesFound);
    }
}
