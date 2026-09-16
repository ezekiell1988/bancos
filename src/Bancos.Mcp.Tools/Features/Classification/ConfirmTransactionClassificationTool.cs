using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Classification;

[McpServerToolType]
public static class ConfirmTransactionClassificationTool
{
    [McpServerTool(Name = "confirm_transaction_classification", Title = "Confirmar clasificación manual de un movimiento",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Registra la categoría confirmada por el usuario para un movimiento y crea o actualiza una regla determinista "
               + "reutilizable (misma cuenta, descripción exacta y tipo de operación) para que futuras coincidencias no requieran "
               + "revisión manual ni IA.")]
    public static async Task<ConfirmTransactionClassificationResult> ConfirmTransactionClassificationAsync(
        ClassificationService service,
        [Description("Movimiento a clasificar.")] Guid transactionId,
        [Description("Categoría confirmada por el usuario.")] Guid categoryId,
        [Description("Lugar o comercio confirmado; se guarda en el movimiento y en la regla reutilizable.")] string? place = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var classification = await service.ConfirmManualClassificationAsync(transactionId, categoryId, place, cancellationToken);
            return new ConfirmTransactionClassificationResult(
                classification.Id,
                classification.CategoryId,
                classification.ClassificationRuleId);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            throw new McpException(ex.Message);
        }
    }
}

public sealed record ConfirmTransactionClassificationResult(Guid ClassificationId, Guid? CategoryId, Guid? RuleId);
