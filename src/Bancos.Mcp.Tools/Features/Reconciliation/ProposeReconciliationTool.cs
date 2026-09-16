using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Reconciliation;

[McpServerToolType]
public static class ProposeReconciliationTool
{
    [McpServerTool(
        Name = "propose_reconciliation",
        Title = "Proponer conciliación de pagos y transferencias",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Calcula una propuesta N:N determinista a partir de partidas seleccionadas y explica montos, fechas y confianza.")]
    public static async Task<ReconciliationResult> ProposeAsync(
        [Description("IDs de las transacciones de pago (al menos una).")] Guid[] paymentTransactionIds,
        [Description("IDs de las transacciones de transferencia (al menos una).")] Guid[] transferTransactionIds,
        ReconciliationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return await service.ProposeAsync(paymentTransactionIds, transferTransactionIds, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new McpException(ex.Message);
        }
    }
}
