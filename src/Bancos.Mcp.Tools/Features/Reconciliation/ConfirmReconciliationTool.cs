using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Reconciliation;

[McpServerToolType]
public static class ConfirmReconciliationTool
{
    [McpServerTool(
        Name = "confirm_reconciliation",
        Title = "Confirmar conciliación",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Confirma una propuesta de conciliación y registra actor, motivo y estado anterior en auditoría.")]
    public static async Task<ReconciliationResult> ConfirmAsync(
        [Description("ID de la conciliación.")] Guid reconciliationId,
        [Description("Actor que realiza la operación.")] string actor,
        [Description("Motivo de la operación.")] string reason,
        ReconciliationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return await service.ConfirmAsync(reconciliationId, actor, reason, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new McpException(ex.Message);
        }
    }
}
