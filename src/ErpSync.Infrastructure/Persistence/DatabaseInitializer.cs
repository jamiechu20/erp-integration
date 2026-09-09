using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ErpSync.Infrastructure.Persistence;

/// <summary>
/// 啟動時自動套用 EF Core Migration（spec.md §13.1）。容器/雲端環境不方便另外跑
/// dotnet ef database update，SQL Server 容器又比 API 慢啟動，所以連不上時重試等待。
/// </summary>
public static class DatabaseInitializer
{
    public static async Task MigrateDatabaseAsync(
        this IServiceProvider services,
        int maxAttempts = 10,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        var delay = retryDelay ?? TimeSpan.FromSeconds(5);
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializer).FullName!);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("資料庫 Migration 套用完成（第 {Attempt} 次嘗試）", attempt);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "套用 Migration 失敗（第 {Attempt}/{MaxAttempts} 次），{Delay} 秒後重試",
                    attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
