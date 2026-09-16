using Bancos.Mcp.Data;
using Bancos.Mcp.Features.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.CardStatements;

public sealed record CardStatementLineSummary(
    Guid TransactionId,
    DateOnly TransactionDate,
    string Description,
    string? Place,
    string CurrencyCode,
    decimal Amount,
    decimal AmountCrc,
    string OperationType);

public sealed record CardStatementSummary(
    Guid Id,
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    DateOnly StatementDate,
    string PeriodLabel,
    DateOnly? MinimumPaymentDueDate,
    DateOnly? CashPaymentDueDate,
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
    IReadOnlyList<CardStatementLineSummary> Lines);

public sealed record CardFinancingSummary(
    Guid Id,
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    string? ReferenceNumber,
    DateOnly FinancingDate,
    string Concept,
    string CurrencyCode,
    decimal InitialBalance,
    decimal OutstandingBalance,
    string Installments,
    decimal InstallmentAmount,
    short? TermMonths,
    decimal? AnnualInterestRate,
    DateOnly? DueDate,
    string Status);

public sealed class CardStatementsQueryService(McpCatalogDbContext db)
{
    public async Task<PagedResult<CardStatementSummary>> ListStatementsAsync(
        Guid? bankAccountId,
        string? periodLabel,
        DateOnly? statementDateFrom,
        DateOnly? statementDateTo,
        int page,
        int itemsPerPage,
        CancellationToken ct = default)
    {
        var query = db.CardStatements.AsNoTracking().AsQueryable();
        if (bankAccountId is not null)
            query = query.Where(statement => statement.BankAccountId == bankAccountId);
        if (!string.IsNullOrWhiteSpace(periodLabel))
            query = query.Where(statement => statement.PeriodLabel == periodLabel);
        if (statementDateFrom is not null)
            query = query.Where(statement => statement.StatementDate >= statementDateFrom);
        if (statementDateTo is not null)
            query = query.Where(statement => statement.StatementDate <= statementDateTo);

        var totalItems = await query.CountAsync(ct);
        var statements = await query
            .Include(statement => statement.BankAccount).ThenInclude(account => account!.Bank)
            .Include(statement => statement.Lines).ThenInclude(line => line.Transaction)
            .OrderByDescending(statement => statement.StatementDate).ThenBy(statement => statement.Id)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .ToListAsync(ct);

        var items = statements.Select(statement => new CardStatementSummary(
            statement.Id,
            statement.BankAccountId,
            statement.BankAccount?.Bank?.Name ?? "Desconocido",
            statement.BankAccount?.Code ?? "Desconocido",
            statement.StatementDate,
            statement.PeriodLabel,
            statement.MinimumPaymentDueDate,
            statement.CashPaymentDueDate,
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
            statement.Lines
                .Where(line => line.Transaction is not null)
                .OrderBy(line => line.Transaction!.TransactionDate)
                .ThenBy(line => line.TransactionId)
                .Select(line => new CardStatementLineSummary(
                    line.Transaction!.Id,
                    line.Transaction.TransactionDate,
                    line.Transaction.Description,
                    line.Transaction.Place,
                    line.Transaction.CurrencyCode,
                    line.Transaction.Amount,
                    line.Transaction.AmountCrc,
                    line.Transaction.OperationType))
                .ToList()))
            .ToList();

        return new PagedResult<CardStatementSummary>(items, page, itemsPerPage, totalItems);
    }

    public async Task<PagedResult<CardFinancingSummary>> ListActiveFinancingsAsync(
        Guid? bankAccountId,
        string? currencyCode,
        int page,
        int itemsPerPage,
        CancellationToken ct = default)
    {
        var query = db.CardFinancings.AsNoTracking().Where(financing => financing.Status == "active");
        if (bankAccountId is not null)
            query = query.Where(financing => financing.BankAccountId == bankAccountId);
        if (!string.IsNullOrWhiteSpace(currencyCode))
            query = query.Where(financing => financing.CurrencyCode == currencyCode);

        var totalItems = await query.CountAsync(ct);
        var financings = await query
            .Include(financing => financing.BankAccount).ThenInclude(account => account!.Bank)
            .OrderByDescending(financing => financing.FinancingDate).ThenBy(financing => financing.Id)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .ToListAsync(ct);

        var items = financings.Select(financing => new CardFinancingSummary(
            financing.Id,
            financing.BankAccountId,
            financing.BankAccount?.Bank?.Name ?? "Desconocido",
            financing.BankAccount?.Code ?? "Desconocido",
            financing.ReferenceNumber,
            financing.FinancingDate,
            financing.Concept,
            financing.CurrencyCode,
            financing.InitialBalance,
            financing.OutstandingBalance,
            financing.Installments,
            financing.InstallmentAmount,
            financing.TermMonths,
            financing.AnnualInterestRate,
            financing.DueDate,
            financing.Status)).ToList();

        return new PagedResult<CardFinancingSummary>(items, page, itemsPerPage, totalItems);
    }
}