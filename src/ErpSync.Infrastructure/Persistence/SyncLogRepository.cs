using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    public async Task<List<SyncLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncLogs
            .AsNoTracking()
            .OrderByDescending(s => s.RunAt)
            .ToListAsync(cancellationToken);
    }
}
