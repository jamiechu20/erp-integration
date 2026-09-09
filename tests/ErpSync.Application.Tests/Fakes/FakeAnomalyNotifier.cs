using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;

namespace ErpSync.Application.Tests.Fakes;

public class FakeAnomalyNotifier : IAnomalyNotifier
{
    /// <summary>設成 false 模擬 SMTP 連不上（spec.md §9.1 的失敗情境）。</summary>
    public bool ShouldSucceed { get; set; } = true;

    public List<Anomaly> NotifiedAnomalies { get; } = new();

    public Task<bool> NotifyAsync(Anomaly anomaly, CancellationToken cancellationToken = default)
    {
        if (!ShouldSucceed)
        {
            return Task.FromResult(false);
        }

        NotifiedAnomalies.Add(anomaly);
        return Task.FromResult(true);
    }
}
