using ErpSync.Application.Interfaces;

namespace ErpSync.Application.Tests.Fakes;

/// <summary>
/// Fake repository 是加進 List 的當下就「落地」了，這裡只記錄被呼叫幾次，
/// 用來驗證同步流程確實有存檔。
/// </summary>
public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(0);
    }
}
