using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;

namespace ErpSync.Application.Tests.Fakes;

public class FakeAnomalyRepository : IAnomalyRepository
{
    public List<Anomaly> Anomalies { get; } = new();

    public Task AddAsync(Anomaly anomaly, CancellationToken cancellationToken = default)
    {
        anomaly.Id = Anomalies.Count + 1;
        Anomalies.Add(anomaly);
        return Task.CompletedTask;
    }

    public Task<List<Anomaly>> GetAllAsync(AnomalyRuleType? ruleType, CancellationToken cancellationToken = default)
        => Task.FromResult(Anomalies.Where(a => ruleType is null || a.RuleType == ruleType).ToList());

    public Task<List<Anomaly>> GetUnnotifiedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Anomalies.Where(a => a.NotifiedAt is null).ToList());
}
