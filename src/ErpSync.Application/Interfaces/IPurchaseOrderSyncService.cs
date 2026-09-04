namespace ErpSync.Application.Interfaces;

public record SyncResult(int RecordsFetched, int RecordsNew, int AnomaliesFound);

public interface IPurchaseOrderSyncService
{
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);
}
