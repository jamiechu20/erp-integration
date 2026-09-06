using ErpSync.Application.DTOs.Responses;
using ErpSync.Application.Interfaces;
using ErpSync.Application.Mapping;
using ErpSync.Domain.Entities;

namespace ErpSync.Application.Services;

public class AnomalyQueryService : IAnomalyQueryService
{
    private readonly IAnomalyRepository _repository;

    public AnomalyQueryService(IAnomalyRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<AnomalyDto>> GetAnomaliesAsync(
        AnomalyRuleType? ruleType, CancellationToken cancellationToken = default)
    {
        var anomalies = await _repository.GetAllAsync(ruleType, cancellationToken);
        return anomalies.Select(ResponseMapper.ToDto).ToList();
    }
}
