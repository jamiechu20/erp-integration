using ErpSync.Application.Interfaces;
using ErpSync.Application.Services;
using ErpSync.Infrastructure.Persistence;
using ErpSync.Infrastructure.Sap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ErpDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ErpDbContext>());
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<ISyncLogRepository, SyncLogRepository>();
        services.AddScoped<IPurchaseOrderSyncService, PurchaseOrderSyncService>();

        services.AddHttpClient<ISapPurchaseOrderClient, SapPurchaseOrderClient>(client =>
        {
            var baseUrl = configuration["SapApi:BaseUrl"]
                ?? throw new InvalidOperationException("設定值 SapApi:BaseUrl 未設定");
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }
}
