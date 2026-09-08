using ErpSync.Application.Interfaces;
using ErpSync.Application.Services;
using ErpSync.Infrastructure.Notifications;
using ErpSync.Infrastructure.Persistence;
using ErpSync.Infrastructure.Sap;
using ErpSync.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace ErpSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ErpDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ErpDbContext>());
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IAnomalyRepository, AnomalyRepository>();
        services.AddScoped<ISyncLogRepository, SyncLogRepository>();
        services.AddScoped<IPurchaseOrderSyncService, PurchaseOrderSyncService>();
        services.AddScoped<IPurchaseOrderQueryService, PurchaseOrderQueryService>();
        services.AddScoped<IAnomalyQueryService, AnomalyQueryService>();
        services.AddScoped<ISyncLogQueryService, SyncLogQueryService>();

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IAnomalyNotifier, SmtpAnomalyNotifier>();

        services.AddHttpClient<ISapPurchaseOrderClient, SapPurchaseOrderClient>(client =>
        {
            var baseUrl = configuration["SapApi:BaseUrl"]
                ?? throw new InvalidOperationException("設定值 SapApi:BaseUrl 未設定");
            client.BaseAddress = new Uri(baseUrl);
        });

        AddSyncScheduling(services, configuration);

        return services;
    }

    // Quartz.NET 排程：定期執行 PurchaseOrderSyncJob（spec.md §8，demo 用 1 分鐘一次）
    private static void AddSyncScheduling(IServiceCollection services, IConfiguration configuration)
    {
        var intervalMinutes = configuration.GetValue<int?>("Sync:IntervalMinutes") ?? 1;
        var jobKey = new JobKey(nameof(PurchaseOrderSyncJob));

        services.AddQuartz(q =>
        {
            q.AddJob<PurchaseOrderSyncJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity($"{nameof(PurchaseOrderSyncJob)}-trigger")
                .WithSimpleSchedule(s => s.WithIntervalInMinutes(intervalMinutes).RepeatForever())
                .StartNow());
        });

        services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);
    }
}
