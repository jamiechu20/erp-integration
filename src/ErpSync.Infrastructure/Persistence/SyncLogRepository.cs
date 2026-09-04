using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;

namespace ErpSync.Infrastructure.Persistence;

public class SyncLogRepository : ISyncLogRepository
{
    private readonly ErpDbContext _dbContext;

    public SyncLogRepository(ErpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SyncLog syncLog, CancellationToken cancellationToken = default)
    {
        await _dbContext.SyncLogs.AddAsync(syncLog, cancellationToken);
    }
}
