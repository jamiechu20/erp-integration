using ErpSync.Domain.Entities;

namespace ErpSync.Application.Interfaces;

/// <summary>
/// 異常通知管道（spec.md §9.1）。實作放在 Infrastructure（MailKit），
/// Application 層只依賴這個介面，之後要換成 Teams/Slack 也不影響同步流程。
/// </summary>
public interface IAnomalyNotifier
{
    /// <summary>寄送單筆異常通知；成功回傳 true，失敗只記 log 並回傳 false，不丟例外。</summary>
    Task<bool> NotifyAsync(Anomaly anomaly, CancellationToken cancellationToken = default);
}
