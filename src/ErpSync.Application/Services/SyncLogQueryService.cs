using ErpSync.Application.DTOs.Responses;
using ErpSync.Application.Interfaces;
using ErpSync.Application.Mapping;

namespace ErpSync.Application.Services;

public class SyncLogQueryService : ISyncLogQueryService
{
    private readonly ISyncLogRepository _repository;

    public SyncLogQueryService(ISyncLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SyncLogDto>> GetSyncLogsAsync(CancellationToken cancellationToken = default)
    {
        var logs = await _repository.GetAllAsync(cancellationToken);
        return logs.Select(ResponseMapper.ToDto).ToList();
    }
}
