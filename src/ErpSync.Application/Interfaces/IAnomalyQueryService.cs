using ErpSync.Application.DTOs.Responses;
using ErpSync.Domain.Entities;

namespace ErpSync.Application.Interfaces;

public interface IAnomalyQueryService
{
    Task<List<AnomalyDto>> GetAnomaliesAsync(
        AnomalyRuleType? ruleType, CancellationToken cancellationToken = default);
}
