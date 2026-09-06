using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSync.Infrastructure.Persistence;

public class AnomalyRepository : IAnomalyRepository
{
    private readonly ErpDbContext _dbContext;

    public AnomalyRepository(ErpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Anomaly anomaly, CancellationToken cancellationToken = default)
    {
        await _dbContext.Anomalies.AddAsync(anomaly, cancellationToken);
    }

    public async Task<List<Anomaly>> GetAllAsync(
        AnomalyRuleType? ruleType, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Anomalies.AsNoTracking().AsQueryable();

        if (ruleType.HasValue)
        {
            query = query.Where(a => a.RuleType == ruleType.Value);
        }

        return await query.OrderByDescending(a => a.DetectedAt).ToListAsync(cancellationToken);
    }
}
