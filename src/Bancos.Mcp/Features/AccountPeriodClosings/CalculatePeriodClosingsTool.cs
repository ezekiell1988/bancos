using System.Text.Json;
using Bancos.Mcp.Data;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;
using Hangfire;
using Microsoft.EntityFrameworkCore;

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
                jobId = new { type = "string" },
                status = new { type = "string" },
                periodId = new { type = "string" },
                periodLabel = new { type = "string" },
                warnings = new { type = "array", items = new { type = "string" } }
            },
            required = new[] { "jobId", "status", "periodId", "periodLabel", "warnings" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("periodId", out var periodIdEl) ||
            !Guid.TryParse(periodIdEl.GetString(), out var periodId))
            return McpToolResult.Error("Se requiere 'periodId' como UUID válido.");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpCatalogDbContext>();
        var period = await db.Periods.FirstOrDefaultAsync(candidate => candidate.Id == periodId, cancellationToken);
        if (period is null)
            return McpToolResult.Error($"Período {periodId} no encontrado.");

        var warnings = new List<string>();
        if (!await db.Transactions.AnyAsync(transaction => transaction.PeriodId == periodId, cancellationToken))
            warnings.Add("El período no tiene movimientos asignados; el job regenerará la asignación de períodos.");

        var jobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        var jobId = jobClient.Enqueue<CalculateAccountPeriodClosingsJob>(
            job => job.ExecuteAsync(periodId, null!));

        var result = new
        {
            status = "enqueued",
            periodId,
            periodLabel = period.Label,
            jobId,
            warnings
        };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}
