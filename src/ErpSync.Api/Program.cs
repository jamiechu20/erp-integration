using ErpSync.Infrastructure;
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

// 前端純 HTML+JS 放在 wwwroot，由同一個 process 提供靜態檔案（spec.md §11）
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
