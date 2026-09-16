using Bancos.Mcp.Features.AccountPeriodClosings;
using Bancos.Mcp.Features.ExchangeRates;
using Bancos.Mcp.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// stdout queda reservado exclusivamente para mensajes MCP; los logs van a stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddDomainServices(builder.Configuration);

builder.Services
    .AddMcpServer(options => options.ServerInfo = new() { Name = "bancos-mcp", Version = "1.0.0" })
    .WithStdioServerTransport()
    .WithToolsFromAssembly(BancosMcpCatalog.Assembly);

var host = builder.Build();

if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    AccountPeriodClosingsModule.ScheduleAccountPeriodClosingsJob();
    ExchangeRatesModule.ScheduleExchangeRatesJob();
}

await host.RunAsync();
