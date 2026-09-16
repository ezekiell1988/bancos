using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;
using Bancos.Mcp.Features.Health;
using Bancos.Mcp.Features.AccountPeriodClosings;
using Bancos.Mcp.Features.ExchangeRates;
using Bancos.Mcp.Features.TemplateDetection;
using Hangfire;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOptions<McpOptions>()
    .BindConfiguration(McpOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDomainServices(builder.Configuration);

builder.Services
    .AddMcpServer(options => options.ServerInfo = new Implementation
    {
        Name = builder.Configuration[$"{McpOptions.Section}:ServerName"] ?? "bancos-mcp",
        Version = builder.Configuration[$"{McpOptions.Section}:ServerVersion"] ?? "1.0.0"
    })
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithToolsFromAssembly(BancosMcpCatalog.Assembly);

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

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/mcp"))
    {
        var options = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<McpOptions>>().Value;
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin) && !options.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }

    await next();
});

app.MapMcp("/mcp").RequireRateLimiting(TemplateDetectionModule.McpToolsRateLimitPolicy);

app.Run();

public partial class Program;
