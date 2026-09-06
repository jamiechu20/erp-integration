using ErpSync.Domain.Entities;

namespace ErpSync.Application.Interfaces;

public interface ISyncLogRepository
{
    Task AddAsync(SyncLog syncLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢同步歷史（spec.md §10）。
    /// </summary>
    Task<List<SyncLog>> GetAllAsync(CancellationToken cancellationToken = default);
}
