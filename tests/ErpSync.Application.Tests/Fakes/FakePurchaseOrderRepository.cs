using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;

namespace ErpSync.Application.Tests.Fakes;

/// <summary>
/// 用一個 List 當「本地 DB」。GetTrackedByIdsAsync 回傳的是 List 裡的同一個物件參考，
/// 模擬 EF Core 追蹤實體的行為——同步服務往 header.Items 加東西就等於寫進了這個假 DB。
/// </summary>
public class FakePurchaseOrderRepository : IPurchaseOrderRepository
{
    public List<PurchaseOrder> Orders { get; } = new();

    public IEnumerable<PurchaseOrderItem> AllItems => Orders.SelectMany(o => o.Items);

    public Task<HashSet<(string PurchaseOrder, string PurchaseOrderItemNumber)>> GetExistingItemKeysAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult(AllItems.Select(i => (i.PurchaseOrder, i.PurchaseOrderItemNumber)).ToHashSet());

    public Task<Dictionary<string, PurchaseOrder>> GetTrackedByIdsAsync(
        IEnumerable<string> purchaseOrderIds, CancellationToken cancellationToken = default)
    {
        var ids = purchaseOrderIds.ToHashSet();
        return Task.FromResult(Orders
            .Where(o => ids.Contains(o.PurchaseOrderNumber))
            .ToDictionary(o => o.PurchaseOrderNumber));
    }

    public Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        Orders.Add(purchaseOrder);
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, (decimal Sum, int Count)>> GetMaterialPriceStatsAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult(AllItems
            .GroupBy(i => i.Material)
            .ToDictionary(g => g.Key, g => (Sum: g.Sum(i => i.NetPriceAmount), Count: g.Count())));

    public Task<List<PurchaseOrder>> GetAllAsync(
        string? supplier, string? companyCode, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("查詢路徑不在本測試範圍（spec.md §12）");

    public Task<PurchaseOrder?> GetByIdWithItemsAsync(
        string purchaseOrderId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("查詢路徑不在本測試範圍（spec.md §12）");
}
