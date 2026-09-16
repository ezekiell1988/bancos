using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Classification;

[McpServerToolType]
public static class ListUnclassifiedTransactionsTool
{
    [McpServerTool(Name = "list_unclassified_transactions", Title = "Listar movimientos No clasificados",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Devuelve los movimientos que nunca alcanzaron una clasificación por regla, IA o confirmación manual, "
               + "junto con la explicación de por qué quedaron pendientes. La respuesta está paginada y no incluye IBAN ni números de tarjeta.")]
    public static async Task<ListUnclassifiedTransactionsResult> ListUnclassifiedTransactionsAsync(
        ClassificationService service,
        [Description("Limita el listado a una cuenta bancaria específica; omite para incluir todas las cuentas.")] Guid? bankAccountId = null,
        [Description("Página a devolver, basada en 1 (por defecto 1).")] int page = 1,
        [Description("Cantidad de movimientos por página (por defecto 50; máximo 200).")] int itemsPerPage = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        itemsPerPage = Math.Clamp(itemsPerPage, 1, 200);

        var summaries = await service.ListUnclassifiedAsync(bankAccountId, page, itemsPerPage, cancellationToken);

        var transactions = summaries.Items.Select(s => new UnclassifiedTransactionItem(
            s.TransactionId,
            s.BankAccountId,
            s.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            s.Description,
            s.Amount,
            s.CurrencyCode,
            s.Explanation)).ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(summaries.TotalItems / (double)summaries.ItemsPerPage));
        return new ListUnclassifiedTransactionsResult(summaries.Page, summaries.ItemsPerPage, summaries.TotalItems, totalPages, transactions);
    }
}

public sealed record UnclassifiedTransactionItem(
    Guid TransactionId,
    Guid BankAccountId,
    string TransactionDate,
    string Description,
    decimal Amount,
    string CurrencyCode,
    string Explanation);

public sealed record ListUnclassifiedTransactionsResult(
    int Page,
    int ItemsPerPage,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<UnclassifiedTransactionItem> Transactions);
