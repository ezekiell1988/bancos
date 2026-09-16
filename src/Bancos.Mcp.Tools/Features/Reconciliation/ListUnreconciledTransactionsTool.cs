using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Reconciliation;

[McpServerToolType]
public static class ListUnreconciledTransactionsTool
{
    [McpServerTool(
        Name = "list_unreconciled_transactions",
        Title = "Listar partidas no conciliadas",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Lista movimientos que todavía no pertenecen a una conciliación confirmada.")]
    public static async Task<ListUnreconciledTransactionsResult> ListAsync(
        ReconciliationService service,
        CancellationToken cancellationToken,
        [Description("Fecha inicial (inclusive), formato yyyy-MM-dd. Omitir para no filtrar.")] DateOnly? dateFrom = null,
        [Description("Fecha final (inclusive), formato yyyy-MM-dd. Omitir para no filtrar.")] DateOnly? dateTo = null,
        [Description("Cantidad máxima de resultados, entre 1 y 200. Por defecto 100.")] int limit = 100)
    {
        var items = await service.ListUnreconciledAsync(dateFrom, dateTo, limit, cancellationToken);
        return new ListUnreconciledTransactionsResult(items, items.Count);
    }
}

public sealed record ListUnreconciledTransactionsResult(
    IReadOnlyList<UnreconciledTransaction> Items,
    int Count);
