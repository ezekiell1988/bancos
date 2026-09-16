using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Transactions;

[McpServerToolType]
public static class SearchTransactionsTool
{
    [McpServerTool(Name = "search_transactions", Title = "Buscar movimientos",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Busca movimientos persistidos filtrando por cuenta bancaria, período, categoría, estado de clasificación y rango de fechas. "
               + "La respuesta está paginada y no incluye IBAN, números de tarjeta ni credenciales.")]
    public static async Task<SearchTransactionsResult> SearchTransactionsAsync(
        TransactionsQueryService service,
        [Description("Limita la búsqueda a una cuenta bancaria específica; omite para incluir todas las cuentas.")] Guid? bankAccountId = null,
        [Description("Limita la búsqueda a un período específico; omite para incluir todos los períodos.")] Guid? periodId = null,
        [Description("Limita la búsqueda a movimientos cuya clasificación más reciente pertenezca a esta categoría; omite para incluir todas las categorías.")] Guid? categoryId = null,
        [Description("Filtra por estado de clasificación: 'classified' (con regla, IA o confirmación manual) o 'unclassified' (sin clasificar). Omite para incluir ambos.")] string? classificationStatus = null,
        [Description("Fecha mínima de movimiento (inclusive), formato yyyy-MM-dd.")] DateOnly? dateFrom = null,
        [Description("Fecha máxima de movimiento (inclusive), formato yyyy-MM-dd.")] DateOnly? dateTo = null,
        [Description("Página a devolver, basada en 1 (por defecto 1).")] int page = 1,
        [Description("Cantidad de movimientos por página (por defecto 50; máximo 200).")] int itemsPerPage = 50,
        CancellationToken cancellationToken = default)
    {
        if (classificationStatus is not null && classificationStatus is not ("classified" or "unclassified"))
            throw new McpException("'classificationStatus' debe ser 'classified' o 'unclassified'.");

        if (dateFrom is not null && dateTo is not null && dateFrom > dateTo)
            throw new McpException("'dateFrom' no puede ser posterior a 'dateTo'.");

        page = Math.Max(page, 1);
        itemsPerPage = Math.Clamp(itemsPerPage, 1, 200);

        var page_ = await service.SearchAsync(bankAccountId, periodId, categoryId, classificationStatus, dateFrom, dateTo, page, itemsPerPage, cancellationToken);

        var transactions = page_.Items.Select(t => new TransactionSearchItem(
            t.TransactionId,
            t.BankAccountId,
            t.BankName,
            t.AccountCode,
            t.PeriodId,
            t.PeriodLabel,
            t.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            t.Description,
            t.Place,
            t.CurrencyCode,
            t.Amount,
            t.AmountCrc,
            t.OperationType,
            t.ClassificationStatus,
            t.CategoryName)).ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(page_.TotalItems / (double)page_.ItemsPerPage));
        return new SearchTransactionsResult(page_.Page, page_.ItemsPerPage, page_.TotalItems, totalPages, transactions);
    }
}

public sealed record TransactionSearchItem(
    Guid TransactionId,
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    Guid? PeriodId,
    string? PeriodLabel,
    string TransactionDate,
    string Description,
    string? Place,
    string CurrencyCode,
    decimal Amount,
    decimal AmountCrc,
    string OperationType,
    string ClassificationStatus,
    string? CategoryName);

public sealed record SearchTransactionsResult(
    int Page,
    int ItemsPerPage,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<TransactionSearchItem> Transactions);
