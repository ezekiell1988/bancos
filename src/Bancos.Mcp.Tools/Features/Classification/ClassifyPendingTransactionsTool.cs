using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Classification;

[McpServerToolType]
public static class ClassifyPendingTransactionsTool
{
    [McpServerTool(Name = "classify_pending_transactions", Title = "Clasificar movimientos pendientes por lote",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Busca movimientos sin ningún intento de clasificación y aplica el motor determinista: "
               + "primero reglas .NET por cuenta y descripción; si no hay coincidencia y la clasificación por IA está habilitada, "
               + "consulta Azure AI solo con la descripción normalizada y el catálogo de categorías permitido. "
               + "Si ninguna de las dos alcanza confianza suficiente, el movimiento queda 'No clasificado' en cola de revisión manual.")]
    public static async Task<ClassifyPendingTransactionsResult> ClassifyPendingTransactionsAsync(
        ClassificationService service,
        [Description("Limita el lote a una cuenta bancaria específica; omite para procesar todas las cuentas.")] Guid? bankAccountId = null,
        [Description("Máximo de movimientos a procesar en esta llamada (por defecto 100; máximo 500).")] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        var summary = await service.ClassifyPendingAsync(bankAccountId, limit, cancellationToken);

        return new ClassifyPendingTransactionsResult(
            summary.Processed,
            new ClassifyPendingTransactionsBySource(summary.Rule, summary.Ai, summary.Unclassified));
    }
}

public sealed record ClassifyPendingTransactionsBySource(int Rule, int Ai, int Unclassified);

public sealed record ClassifyPendingTransactionsResult(int Processed, ClassifyPendingTransactionsBySource BySource);
