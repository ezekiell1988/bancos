using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.CardStatements;

[McpServerToolType]
public static class ListCardFinancingsTool
{
    [McpServerTool(Name = "list_card_financings", Title = "Listar financiamientos de tarjeta",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lista financiamientos activos de tarjetas con saldos, cuotas, tasas y vencimientos. No incluye archivos fuente ni huellas de importación.")]
    public static async Task<ListCardFinancingsResult> ListCardFinancingsAsync(
        CardStatementsQueryService service,
        [Description("Cuenta de tarjeta a consultar.")] Guid? bankAccountId = null,
        [Description("Moneda del financiamiento.")] string? currencyCode = null,
        [Description("Página basada en 1; por defecto 1.")] int page = 1,
        [Description("Cantidad por página; por defecto 50.")] int itemsPerPage = 50,
        CancellationToken cancellationToken = default)
    {
        if (currencyCode is not null && currencyCode is not ("CRC" or "USD"))
            throw new McpException("'currencyCode' debe ser 'CRC' o 'USD'.");

        page = Math.Max(page, 1);
        itemsPerPage = Math.Clamp(itemsPerPage, 1, 200);

        var pageResult = await service.ListActiveFinancingsAsync(bankAccountId, currencyCode, page, itemsPerPage, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(pageResult.TotalItems / (double)pageResult.ItemsPerPage));

        var financings = pageResult.Items.Select(financing => new CardFinancingItem(
            financing.Id,
            financing.BankAccountId,
            financing.BankName,
            financing.AccountCode,
            financing.ReferenceNumber,
            financing.FinancingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            financing.Concept,
            financing.CurrencyCode,
            financing.InitialBalance,
            financing.OutstandingBalance,
            financing.Installments,
            financing.InstallmentAmount,
            financing.TermMonths,
            financing.AnnualInterestRate,
            financing.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            financing.Status)).ToList();

        return new ListCardFinancingsResult(pageResult.Page, pageResult.ItemsPerPage, pageResult.TotalItems, totalPages, financings);
    }
}

public sealed record CardFinancingItem(
    Guid CardFinancingId,
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    string? ReferenceNumber,
    string FinancingDate,
    string Concept,
    string CurrencyCode,
    decimal InitialBalance,
    decimal OutstandingBalance,
    string Installments,
    decimal InstallmentAmount,
    short? TermMonths,
    decimal? AnnualInterestRate,
    string? DueDate,
    string Status);

public sealed record ListCardFinancingsResult(
    int Page,
    int ItemsPerPage,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<CardFinancingItem> Financings);
