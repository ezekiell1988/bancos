using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Ledger;

public sealed class GetLedgerPeriodTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "get_ledger_period",
        Title: "Consultar libro mayor por período",
        Description: "Devuelve los comprobantes y líneas trazables de los movimientos registrados en un período. "
                   + "Cada movimiento importado se representa como un comprobante de una línea porque el modelo actual conserva el auxiliar bancario, no asientos de doble partida.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                periodId = new
                {
                    type = "string",
                    format = "uuid",
                    description = "ID del período que se desea consultar."
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
                vouchers = new { type = "array" },
                warnings = new { type = "array", items = new { type = "string" } }
            },
            required = new[] { "status", "periodId", "periodLabel", "periodStart", "periodEnd", "vouchers", "warnings" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("periodId", out var periodIdElement)
            || !Guid.TryParse(periodIdElement.GetString(), out var periodId))
            return McpToolResult.Error("Se requiere 'periodId' como UUID válido.");

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<LedgerQueryService>();
        var ledger = await service.GetPeriodAsync(periodId, cancellationToken);
        if (ledger is null)
            return McpToolResult.Error($"Período {periodId} no encontrado.");

        var result = new
        {
            status = ledger.Warnings.Count == 0 ? "completed" : "completed_with_warnings",
            periodId = ledger.PeriodId,
            periodLabel = ledger.PeriodLabel,
            periodStart = ledger.PeriodStart,
            periodEnd = ledger.PeriodEnd,
            vouchers = ledger.Vouchers,
            warnings = ledger.Warnings
        };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}