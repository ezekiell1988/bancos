using Bancos.Mcp.Tools;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace Bancos.Mcp.Features.TemplateDetection;

public static class TemplateDetectionModule
{
    public const string McpToolsRateLimitPolicy = "mcp-tools";

    public static IServiceCollection AddTemplateDetectionModule(this IServiceCollection services, IConfiguration configuration) => services
        .AddOptions<FileTemplateDetectionOptions>().BindConfiguration(FileTemplateDetectionOptions.Section).ValidateDataAnnotations().ValidateOnStart().Services
        .AddSingleton<ImportTemplateDetectionService>()
        .AddSingleton<IMcpTool, DetectImportTemplateTool>()
        .AddRateLimiter(options =>
        {
            var limits = configuration.GetSection(McpToolRateLimitOptions.Section).Get<McpToolRateLimitOptions>() ?? new();
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(context.Request.Path.Value ?? "/", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.RequestPermitLimit,
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(1),
                    AutoReplenishment = true
                }));
            options.AddPolicy(McpToolsRateLimitPolicy, _ => RateLimitPartition.GetConcurrencyLimiter("mcp", _ =>
                new ConcurrencyLimiterOptions { PermitLimit = limits.PermitLimit, QueueLimit = limits.QueueLimit, QueueProcessingOrder = QueueProcessingOrder.OldestFirst }));
        });

    public static IEndpointRouteBuilder MapMcpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/mcp/sse", Protocol.McpSseHandler.HandleSseAsync);
        endpoints.MapPost("/mcp/sse/message", (HttpRequest req, Tools.ToolRegistry reg, IOptions<Protocol.McpOptions> opt, string sessionId, CancellationToken ct) =>
            Protocol.McpSseHandler.HandleMessageAsync(req, reg, opt, sessionId, ct));
        endpoints.MapPost("/{**path}", Protocol.McpHandler.HandleAsync).RequireRateLimiting(McpToolsRateLimitPolicy);
        endpoints.MapGet("/{**path}", Protocol.McpHandler.GetHealth);
        return endpoints;
    }
}

public sealed class McpToolRateLimitOptions
{
    public const string Section = "McpToolRateLimit";
    public int PermitLimit { get; init; } = 2;
    public int QueueLimit { get; init; } = 0;
    public int RequestPermitLimit { get; init; } = 60;
}
