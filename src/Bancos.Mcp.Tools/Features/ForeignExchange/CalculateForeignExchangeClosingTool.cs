using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.ForeignExchange;

[McpServerToolType]
public static class CalculateForeignExchangeClosingTool
{
    [McpServerTool(
        Name = "calculate_foreign_exchange_closing",
        Title = "Calcular cierre cambiario de pasivos USD",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Calcula el diferencial cambiario mensual regenerable de pasivos USD. Solo considera cuentas de crédito y préstamos; activos USD quedan fuera de alcance.")]
    public static async Task<ForeignExchangeClosing> CalculateAsync(
        [Description("ID del período para el cierre cambiario.")] Guid periodId,
        ForeignExchangeService service,
        CancellationToken cancellationToken)
    {
        var closing = await service.CalculateAsync(periodId, cancellationToken);
        if (closing is null)
            throw new McpException($"Período {periodId} no encontrado.");

        return closing;
    }
}
