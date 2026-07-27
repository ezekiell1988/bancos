using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;
using Hangfire;

namespace Bancos.Mcp.Features.AccountPeriodClosings;

public sealed class CalculatePeriodClosingsTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "calculate_period_closings",
        Title: "Calcular cierres de saldo por periodo",
        Description: "Encola un job Hangfire que calcula y persiste el saldo acumulado por cuenta bancaria "
                   + "desde el periodo indicado hacia adelante. Retorna el ID del job encolado.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                periodId = new
                {
                    type = "string",
                    format = "uuid",
                    description = "ID del periodo desde el cual calcular los cierres (inclusive)."
                }
            },
            required = new[] { "periodId" },
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                jobId = new { type = "string" }
            },
            required = new[] { "jobId" },
            additionalProperties = false
        });

    public ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("periodId", out var periodIdEl) ||
            !Guid.TryParse(periodIdEl.GetString(), out var periodId))
            return ValueTask.FromResult(McpToolResult.Error("Se requiere 'periodId' como UUID válido."));

        using var scope = scopeFactory.CreateScope();
        var jobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        var jobId = jobClient.Enqueue<CalculateAccountPeriodClosingsJob>(
            job => job.ExecuteAsync(periodId, null!));

        var result = new { jobId };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return ValueTask.FromResult(new McpToolResult([McpContent.FromText(json)], result));
    }
}
