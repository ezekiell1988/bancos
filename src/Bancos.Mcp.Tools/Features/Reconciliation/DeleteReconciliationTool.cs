using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Reconciliation;

[McpServerToolType]
public static class DeleteReconciliationTool
{
    [McpServerTool(
        Name = "delete_reconciliation",
        Title = "Eliminar conciliación",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Marca una conciliación como eliminada, conserva sus movimientos y registra la operación en auditoría.")]
    public static async Task<ReconciliationResult> DeleteAsync(
        [Description("ID de la conciliación.")] Guid reconciliationId,
        [Description("Actor que realiza la operación.")] string actor,
        [Description("Motivo de la operación.")] string reason,
        ReconciliationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return await service.DeleteAsync(reconciliationId, actor, reason, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new McpException(ex.Message);
        }
    }
}
