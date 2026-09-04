using ErpSync.Domain.Entities;

namespace ErpSync.Application.Interfaces;

public interface ISyncLogRepository
{
    Task AddAsync(SyncLog syncLog, CancellationToken cancellationToken = default);
}
