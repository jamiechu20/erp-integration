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

    public async Task<Dictionary<string, (decimal Sum, int Count)>> GetMaterialPriceStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var stats = await _dbContext.PurchaseOrderItems
            .AsNoTracking()
            .GroupBy(i => i.Material)
            .Select(g => new { Material = g.Key, Sum = g.Sum(i => i.NetPriceAmount), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return stats.ToDictionary(s => s.Material, s => (s.Sum, s.Count));
    }

    public async Task<List<PurchaseOrder>> GetAllAsync(
        string? supplier, string? companyCode, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PurchaseOrders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(supplier))
        {
            query = query.Where(o => o.Supplier == supplier);
        }

        if (!string.IsNullOrWhiteSpace(companyCode))
        {
            query = query.Where(o => o.CompanyCode == companyCode);
        }

        return await query.OrderBy(o => o.PurchaseOrderNumber).ToListAsync(cancellationToken);
    }

    public async Task<PurchaseOrder?> GetByIdWithItemsAsync(
        string purchaseOrderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PurchaseOrderNumber == purchaseOrderId, cancellationToken);
    }
}
