using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;
using Bancos.Mcp.Data;
using Bancos.Mcp.Features.Health;
using Bancos.Mcp.Features.TemplateDetection;
using Bancos.Mcp.Features.AccountPeriodClosings;
using Bancos.Mcp.Features.Classification;
using Bancos.Mcp.Features.FileProcessing;
using Bancos.Mcp.Features.ExchangeRates;
using Hangfire;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.AddHealthModule();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
    builder.Services.AddDbContext<McpCatalogDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddOptions<McpOptions>()
    .BindConfiguration(McpOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IMcpTool, StatusTool>();
builder.Services.AddSingleton<ToolRegistry>();
builder.Services.AddTemplateDetectionModule(builder.Configuration);
builder.Services.AddFileProcessingModule(builder.Configuration);
builder.Services.AddAccountPeriodClosingsModule();
builder.Services.AddClassificationModule();
builder.Services.AddExchangeRatesModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
    app.UseHangfireDashboard("/hangfire");
    app.MapAccountPeriodClosingsEndpoints();
    app.UseAccountPeriodClosingsJobs();
    app.UseExchangeRatesJobs();
}

app.MapHealthEndpoints();
app.MapMcpEndpoints();

app.Run();

public partial class Program;
