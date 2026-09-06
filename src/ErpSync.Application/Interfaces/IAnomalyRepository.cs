using ErpSync.Domain.Entities;

namespace ErpSync.Application.Interfaces;

public interface IAnomalyRepository
{
    Task AddAsync(Anomaly anomaly, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢異常清單，可用 ruleType 篩選（spec.md §10）。
    /// </summary>
    Task<List<Anomaly>> GetAllAsync(AnomalyRuleType? ruleType, CancellationToken cancellationToken = default);
}
