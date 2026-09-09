using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;

namespace ErpSync.Application.Tests.Fakes;

public class FakeSyncLogRepository : ISyncLogRepository
{
    public List<SyncLog> SyncLogs { get; } = new();

    public Task AddAsync(SyncLog syncLog, CancellationToken cancellationToken = default)
    {
        syncLog.Id = SyncLogs.Count + 1;
        SyncLogs.Add(syncLog);
        return Task.CompletedTask;
    }

    public Task<List<SyncLog>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(SyncLogs.ToList());
}
