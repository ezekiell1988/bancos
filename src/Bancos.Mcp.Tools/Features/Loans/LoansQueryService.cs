using Bancos.Mcp.Data;
using Bancos.Mcp.Features.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.Loans;

public sealed record LoanPaymentSummary(
    Guid Id,
    int InstallmentNumber,
    DateOnly PaymentDate,
    decimal Capital,
    decimal Interest,
    decimal LateFee,
    decimal OtherCharges,
    decimal Total,
    decimal Balance,
    string Status);

public sealed record LoanStatementSummary(
    Guid Id,
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    DateOnly StatementDate,
    string CurrencyCode,
    string? LoanNumber,
    decimal? OriginalLoanAmount,
    decimal? InterestRate,
    int? TermMonths,
    DateOnly? StartDate,
    DateOnly? MaturityDate,
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
    IReadOnlyList<LoanPaymentSummary> Payments);

public sealed class LoansQueryService(McpCatalogDbContext db)
{
    public async Task<PagedResult<LoanStatementSummary>> ListStatementsAsync(
        Guid? bankAccountId,
        string? loanNumber,
        DateOnly? statementDateFrom,
        DateOnly? statementDateTo,
        int page,
        int itemsPerPage,
        CancellationToken ct = default)
    {
        var query = db.LoanStatements.AsNoTracking().AsQueryable();
        if (bankAccountId is not null)
            query = query.Where(statement => statement.BankAccountId == bankAccountId);
        if (!string.IsNullOrWhiteSpace(loanNumber))
            query = query.Where(statement => statement.LoanNumber == loanNumber);
        if (statementDateFrom is not null)
            query = query.Where(statement => statement.StatementDate >= statementDateFrom);
        if (statementDateTo is not null)
            query = query.Where(statement => statement.StatementDate <= statementDateTo);

        var totalItems = await query.CountAsync(ct);
        var statements = await query
            .Include(statement => statement.BankAccount).ThenInclude(account => account!.Bank)
            .Include(statement => statement.Payments)
            .OrderByDescending(statement => statement.StatementDate).ThenBy(statement => statement.Id)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .ToListAsync(ct);

        var items = statements.Select(statement => new LoanStatementSummary(
            statement.Id,
            statement.BankAccountId,
            statement.BankAccount?.Bank?.Name ?? "Desconocido",
            statement.BankAccount?.Code ?? "Desconocido",
            statement.StatementDate,
            statement.CurrencyCode,
            statement.LoanNumber,
            statement.OriginalLoanAmount,
            statement.InterestRate,
            statement.TermMonths,
            statement.StartDate,
            statement.MaturityDate,
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
            statement.Payments
                .OrderBy(payment => payment.PaymentDate)
                .ThenBy(payment => payment.InstallmentNumber)
                .ThenBy(payment => payment.Id)
                .Select(payment => new LoanPaymentSummary(
                    payment.Id,
                    payment.InstallmentNumber,
                    payment.PaymentDate,
                    payment.Capital,
                    payment.Interest,
                    payment.LateFee,
                    payment.OtherCharges,
                    payment.Total,
                    payment.Balance,
                    payment.Status))
                .ToList()))
            .ToList();

        return new PagedResult<LoanStatementSummary>(items, page, itemsPerPage, totalItems);
    }
}