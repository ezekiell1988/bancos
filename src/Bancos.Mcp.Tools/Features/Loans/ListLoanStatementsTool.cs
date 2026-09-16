using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Loans;

[McpServerToolType]
public static class ListLoanStatementsTool
{
    [McpServerTool(Name = "list_loan_statements", Title = "Consultar extractos de préstamos",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Consulta extractos de préstamos con saldos, fechas, porciones corriente y largo plazo, y cuotas del calendario. No incluye archivos fuente ni huellas de importación.")]
    public static async Task<ListLoanStatementsResult> ListLoanStatementsAsync(
        LoansQueryService service,
        [Description("Cuenta de préstamo a consultar.")] Guid? bankAccountId = null,
        [Description("Número de operación del préstamo.")] string? loanNumber = null,
        [Description("Fecha mínima del extracto, formato yyyy-MM-dd.")] DateOnly? statementDateFrom = null,
        [Description("Fecha máxima del extracto, formato yyyy-MM-dd.")] DateOnly? statementDateTo = null,
        [Description("Página basada en 1; por defecto 1.")] int page = 1,
        [Description("Cantidad por página; por defecto 50.")] int itemsPerPage = 50,
        CancellationToken cancellationToken = default)
    {
        if (statementDateFrom is not null && statementDateTo is not null && statementDateFrom > statementDateTo)
            throw new McpException("'statementDateFrom' no puede ser posterior a 'statementDateTo'.");

        page = Math.Max(page, 1);
        itemsPerPage = Math.Clamp(itemsPerPage, 1, 200);

        var pageResult = await service.ListStatementsAsync(bankAccountId, loanNumber, statementDateFrom, statementDateTo, page, itemsPerPage, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(pageResult.TotalItems / (double)pageResult.ItemsPerPage));

        var statements = pageResult.Items.Select(statement => new LoanStatementItem(
            statement.Id,
            statement.BankAccountId,
            statement.BankName,
            statement.AccountCode,
            FormatDate(statement.StatementDate),
            statement.CurrencyCode,
            statement.LoanNumber,
            statement.OriginalLoanAmount,
            statement.InterestRate,
            statement.TermMonths,
            FormatDate(statement.StartDate),
            FormatDate(statement.MaturityDate),
            statement.OutstandingBalance,
            statement.NextMonthCapital,
            statement.NextMonthInterest,
            statement.NextMonthTotal,
            statement.CurrentPortionCapital,
            statement.CurrentPortionInterest,
            statement.CurrentPortionTotal,
            statement.LongTermCapital,
            statement.LongTermInterest,
            statement.LongTermTotal,
            statement.Payments.Select(payment => new LoanPaymentItem(
                payment.Id,
                payment.InstallmentNumber,
                FormatDate(payment.PaymentDate),
                payment.Capital,
                payment.Interest,
                payment.LateFee,
                payment.OtherCharges,
                payment.Total,
                payment.Balance,
                payment.Status)).ToList())).ToList();

        return new ListLoanStatementsResult(pageResult.Page, pageResult.ItemsPerPage, pageResult.TotalItems, totalPages, statements);
    }

    private static string? FormatDate(DateOnly? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public sealed record LoanPaymentItem(
    Guid LoanPaymentId,
    int InstallmentNumber,
    string PaymentDate,
    decimal Capital,
    decimal Interest,
    decimal LateFee,
    decimal OtherCharges,
    decimal Total,
    decimal Balance,
    string Status);

public sealed record LoanStatementItem(
    Guid LoanStatementId,
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    string StatementDate,
    string CurrencyCode,
    string? LoanNumber,
    decimal? OriginalLoanAmount,
    decimal? InterestRate,
    int? TermMonths,
    string? StartDate,
    string? MaturityDate,
    decimal OutstandingBalance,
    decimal? NextMonthCapital,
    decimal? NextMonthInterest,
    decimal? NextMonthTotal,
    decimal? CurrentPortionCapital,
    decimal? CurrentPortionInterest,
    decimal? CurrentPortionTotal,
    decimal? LongTermCapital,
    decimal? LongTermInterest,
    decimal? LongTermTotal,
    IReadOnlyList<LoanPaymentItem> Payments);

public sealed record ListLoanStatementsResult(
    int Page,
    int ItemsPerPage,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<LoanStatementItem> Statements);
