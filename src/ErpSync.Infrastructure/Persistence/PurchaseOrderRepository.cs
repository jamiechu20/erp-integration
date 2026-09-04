using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSync.Infrastructure.Persistence;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly ErpDbContext _dbContext;

    public PurchaseOrderRepository(ErpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HashSet<(string PurchaseOrder, string PurchaseOrderItemNumber)>> GetExistingItemKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var keys = await _dbContext.PurchaseOrderItems
            .AsNoTracking()
            .Select(i => new { i.PurchaseOrder, i.PurchaseOrderItemNumber })
            .ToListAsync(cancellationToken);

        return keys.Select(k => (k.PurchaseOrder, k.PurchaseOrderItemNumber)).ToHashSet();
    }

    public async Task<Dictionary<string, PurchaseOrder>> GetTrackedByIdsAsync(
        IEnumerable<string> purchaseOrderIds, CancellationToken cancellationToken = default)
    {
        var ids = purchaseOrderIds.Distinct().ToList();

        var headers = await _dbContext.PurchaseOrders
            .Include(o => o.Items)
            .Where(o => ids.Contains(o.PurchaseOrderNumber))
            .ToListAsync(cancellationToken);

        return headers.ToDictionary(o => o.PurchaseOrderNumber);
    }

    public async Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        await _dbContext.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken);
    }
}
