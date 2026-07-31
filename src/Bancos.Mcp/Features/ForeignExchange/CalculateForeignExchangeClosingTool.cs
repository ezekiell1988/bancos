using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.ForeignExchange;

public sealed class CalculateForeignExchangeClosingTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "calculate_foreign_exchange_closing",
        Title: "Calcular cierre cambiario de pasivos USD",
        Description: "Calcula el diferencial cambiario mensual regenerable de pasivos USD. "
                   + "Solo considera cuentas de crédito y préstamos; activos USD quedan fuera de alcance.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                periodId = new
                {
                    type = "string",
                    format = "uuid",
                    description = "ID del período para el cierre cambiario."
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
                status = new { type = "string" },
                periodId = new { type = "string" },
                periodLabel = new { type = "string" },
                periodStart = new { type = "string" },
                periodEnd = new { type = "string" },
                totalDifferenceCrc = new { type = "number" },
                lines = new { type = "array" },
                warnings = new { type = "array", items = new { type = "string" } }
            },
            required = new[] { "status", "periodId", "periodLabel", "periodStart", "periodEnd", "totalDifferenceCrc", "lines", "warnings" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("periodId", out var periodIdElement)
            || !Guid.TryParse(periodIdElement.GetString(), out var periodId))
            return McpToolResult.Error("Se requiere 'periodId' como UUID válido.");

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ForeignExchangeService>();
        var closing = await service.CalculateAsync(periodId, cancellationToken);
        if (closing is null)
            return McpToolResult.Error($"Período {periodId} no encontrado.");

        var result = new
        {
            status = closing.Status,
            periodId = closing.PeriodId,
            periodLabel = closing.PeriodLabel,
            periodStart = closing.PeriodStart,
            periodEnd = closing.PeriodEnd,
            totalDifferenceCrc = closing.TotalDifferenceCrc,
            lines = closing.Lines,
            warnings = closing.Warnings
        };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}