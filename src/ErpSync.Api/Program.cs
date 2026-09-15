using ErpSync.Infrastructure;
using ErpSync.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog：Console + 逐日 rolling 檔案（spec.md §9.2），設定放在 appsettings.json 的 Serilog 節點
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// 容器/雲端環境用 Database:AutoMigrate=true 讓 API 啟動時自動建表（spec.md §13.1）
if (app.Configuration.GetValue("Database:AutoMigrate", false))
{
    await app.Services.MigrateDatabaseAsync();
}

// Demo 專案，API 文件（/openapi/v1.json、/scalar）不分環境都開放，方便展示
app.MapOpenApi();
app.MapScalarApiReference();

app.UseSerilogRequestLogging();

// 容器內只跑 HTTP（沒有憑證），用設定關掉轉址（spec.md §13.1）
if (app.Configuration.GetValue("EnableHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}

// 前端純 HTML+JS 放在 wwwroot，由同一個 process 提供靜態檔案（spec.md §11）
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
