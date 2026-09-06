using ErpSync.Application.DTOs.Responses;

namespace ErpSync.Application.Interfaces;

public interface ISyncLogQueryService
{
    Task<List<SyncLogDto>> GetSyncLogsAsync(CancellationToken cancellationToken = default);
}
