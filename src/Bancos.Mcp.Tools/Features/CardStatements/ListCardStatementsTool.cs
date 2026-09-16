using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.CardStatements;

[McpServerToolType]
public static class ListCardStatementsTool
{
    [McpServerTool(Name = "list_card_statements", Title = "Consultar cortes de tarjeta",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Consulta cortes de tarjeta con saldos, fechas de pago y movimientos vinculados. La respuesta no incluye archivos fuente ni huellas de importación.")]
    public static async Task<ListCardStatementsResult> ListCardStatementsAsync(
        CardStatementsQueryService service,
        [Description("Cuenta de tarjeta a consultar.")] Guid? bankAccountId = null,
        [Description("Etiqueta exacta del período informativo, por ejemplo JUL-2026.")] string? periodLabel = null,
        [Description("Fecha mínima de corte, formato yyyy-MM-dd.")] DateOnly? statementDateFrom = null,
        [Description("Fecha máxima de corte, formato yyyy-MM-dd.")] DateOnly? statementDateTo = null,
        [Description("Página basada en 1; por defecto 1.")] int page = 1,
        [Description("Cantidad por página; por defecto 50.")] int itemsPerPage = 50,
        CancellationToken cancellationToken = default)
    {
        if (statementDateFrom is not null && statementDateTo is not null && statementDateFrom > statementDateTo)
            throw new McpException("'statementDateFrom' no puede ser posterior a 'statementDateTo'.");

        page = Math.Max(page, 1);
        itemsPerPage = Math.Clamp(itemsPerPage, 1, 200);

        var pageResult = await service.ListStatementsAsync(bankAccountId, periodLabel, statementDateFrom, statementDateTo, page, itemsPerPage, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(pageResult.TotalItems / (double)pageResult.ItemsPerPage));

        var statements = pageResult.Items.Select(statement => new CardStatementItem(
            statement.Id,
            statement.BankAccountId,
            statement.BankName,
            statement.AccountCode,
            FormatDate(statement.StatementDate),
            statement.PeriodLabel,
            FormatDate(statement.MinimumPaymentDueDate),
            FormatDate(statement.CashPaymentDueDate),
            statement.PreviousBalanceCrc,
            statement.PreviousBalanceUsd,
            statement.PurchasesTotalCrc,
            statement.PurchasesTotalUsd,
            statement.PaymentsTotalCrc,
            statement.PaymentsTotalUsd,
            statement.InterestTotalCrc,
            statement.InterestTotalUsd,
            statement.CurrentBalanceCrc,
            statement.CurrentBalanceUsd,
            statement.MinimumPaymentCrc,
            statement.MinimumPaymentUsd,
            statement.CashPaymentCrc,
            statement.CashPaymentUsd,
            statement.CreditLimitCrc,
            statement.CreditLimitUsd,
            statement.AvailableBalanceCrc,
            statement.AvailableBalanceUsd,
            statement.Lines.Select(line => new CardStatementLineItem(
                line.TransactionId,
                FormatDate(line.TransactionDate),
                line.Description,
                line.Place,
                line.CurrencyCode,
                line.Amount,
                line.AmountCrc,
                line.OperationType)).ToList())).ToList();

        return new ListCardStatementsResult(pageResult.Page, pageResult.ItemsPerPage, pageResult.TotalItems, totalPages, statements);
    }

    private static string? FormatDate(DateOnly? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public sealed record CardStatementLineItem(
    Guid TransactionId,
    string TransactionDate,
    string Description,
    string? Place,
    string CurrencyCode,
    decimal Amount,
    decimal AmountCrc,
    string OperationType);

public sealed record CardStatementItem(
    Guid CardStatementId,
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    string StatementDate,
    string PeriodLabel,
    string? MinimumPaymentDueDate,
    string? CashPaymentDueDate,
    decimal PreviousBalanceCrc,
    decimal PreviousBalanceUsd,
    decimal PurchasesTotalCrc,
    decimal PurchasesTotalUsd,
    decimal PaymentsTotalCrc,
    decimal PaymentsTotalUsd,
    decimal InterestTotalCrc,
    decimal InterestTotalUsd,
    decimal CurrentBalanceCrc,
    decimal CurrentBalanceUsd,
    decimal MinimumPaymentCrc,
    decimal MinimumPaymentUsd,
    decimal CashPaymentCrc,
    decimal CashPaymentUsd,
    decimal CreditLimitCrc,
    decimal CreditLimitUsd,
    decimal AvailableBalanceCrc,
    decimal AvailableBalanceUsd,
    IReadOnlyList<CardStatementLineItem> Lines);

public sealed record ListCardStatementsResult(
    int Page,
    int ItemsPerPage,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<CardStatementItem> Statements);
