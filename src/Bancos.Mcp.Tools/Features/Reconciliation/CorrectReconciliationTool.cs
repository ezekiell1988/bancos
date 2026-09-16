using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Reconciliation;

[McpServerToolType]
public static class CorrectReconciliationTool
{
    [McpServerTool(
        Name = "correct_reconciliation",
        Title = "Corregir conciliación",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Reemplaza las partidas asociadas a una conciliación, conserva los movimientos originales y registra la corrección.")]
    public static async Task<ReconciliationResult> CorrectAsync(
        [Description("ID de la conciliación.")] Guid reconciliationId,
        [Description("IDs de las transacciones de pago (al menos una).")] Guid[] paymentTransactionIds,
        [Description("IDs de las transacciones de transferencia (al menos una).")] Guid[] transferTransactionIds,
        [Description("Actor que realiza la operación.")] string actor,
        [Description("Motivo de la operación.")] string reason,
        ReconciliationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return await service.CorrectAsync(reconciliationId, paymentTransactionIds, transferTransactionIds, actor, reason, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new McpException(ex.Message);
        }
    }
}
