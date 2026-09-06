using ErpSync.Domain.Entities;

namespace ErpSync.Application.Interfaces;

public interface IPurchaseOrderRepository
{
    /// <summary>
    /// 已存在的品項複合鍵（PurchaseOrder, PurchaseOrderItemNumber），用來判斷同步進來的
    /// 品項是不是新資料。
    /// </summary>
    Task<HashSet<(string PurchaseOrder, string PurchaseOrderItemNumber)>> GetExistingItemKeysAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 依 PurchaseOrder id 取出已被 EF Core 追蹤的 Header（含 Items），供同步時直接在
    /// 記憶體中掛上新品項，交由 SaveChanges 一併寫入。
    /// </summary>
    Task<Dictionary<string, PurchaseOrder>> GetTrackedByIdsAsync(
        IEnumerable<string> purchaseOrderIds, CancellationToken cancellationToken = default);

    Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);

    /// <summary>
    /// 各 Material 目前已存在品項的單價加總與筆數，供價格異常規則（spec.md §8 規則二）計算歷史均價。
    /// </summary>
    Task<Dictionary<string, (decimal Sum, int Count)>> GetMaterialPriceStatsAsync(
        CancellationToken cancellationToken = default);
}
