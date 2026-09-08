using ErpSync.Domain.Entities;

namespace ErpSync.Application.Interfaces;

public interface IAnomalyRepository
{
    Task AddAsync(Anomaly anomaly, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢異常清單，可用 ruleType 篩選（spec.md §10）。
    /// </summary>
    Task<List<Anomaly>> GetAllAsync(AnomalyRuleType? ruleType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取出所有尚未通知（NotifiedAt IS NULL）的異常，供同步流程寄送通知（spec.md §9.1）。
    /// 這裡回傳被 EF 追蹤的實體，呼叫端改完 NotifiedAt 直接 SaveChanges 即可。
    /// </summary>
    Task<List<Anomaly>> GetUnnotifiedAsync(CancellationToken cancellationToken = default);
}
