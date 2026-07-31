using Bancos.Mcp.Data;
using Bancos.Mcp.Features.Accounts;
using Bancos.Mcp.Features.Classification;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.Transactions;

public sealed record TransactionSummary(
    Guid TransactionId,
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    Guid? PeriodId,
    string? PeriodLabel,
    DateOnly TransactionDate,
    string Description,
    string? Place,
    string CurrencyCode,
    decimal Amount,
    decimal AmountCrc,
    string OperationType,
    string ClassificationStatus,
    string? CategoryName);

public sealed record TransactionClassificationEntry(
    Guid Id,
    string Source,
    string? CategoryCode,
    string? CategoryName,
    decimal? Confidence,
    string? Explanation,
    DateTimeOffset CreatedAt,
    string? RuleDescriptionPattern);

public sealed record TransactionDetail(
    Guid TransactionId,
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    Guid? PeriodId,
    string? PeriodLabel,
    string? ReferenceNumber,
    DateOnly TransactionDate,
    DateOnly? PaymentDate,
    string Description,
    string? Place,
    string CurrencyCode,
    decimal Amount,
    decimal AmountCrc,
    decimal? ExchangeRate,
    string OperationType,
    IReadOnlyList<TransactionClassificationEntry> Classifications);

public sealed class TransactionsQueryService(McpCatalogDbContext db)
{
    public async Task<PagedResult<TransactionSummary>> SearchAsync(
        Guid? bankAccountId,
        Guid? periodId,
        Guid? categoryId,
        string? classificationStatus,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int page,
        int itemsPerPage,
        CancellationToken ct = default)
    {
        var query = db.Transactions.AsQueryable();
        if (bankAccountId is not null)
            query = query.Where(t => t.BankAccountId == bankAccountId);
        if (periodId is not null)
            query = query.Where(t => t.PeriodId == periodId);
        if (dateFrom is not null)
            query = query.Where(t => t.TransactionDate >= dateFrom);
        if (dateTo is not null)
            query = query.Where(t => t.TransactionDate <= dateTo);
        if (classificationStatus == "unclassified")
            query = query.Where(t => !t.Classifications.Any(c => c.Source != ClassificationSource.Unclassified));
        else if (classificationStatus == "classified")
            query = query.Where(t => t.Classifications.Any(c => c.Source != ClassificationSource.Unclassified));
        if (categoryId is not null)
            query = query.Where(t => db.TransactionClassifications
                .Where(c => c.TransactionId == t.Id)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => c.CategoryId)
                .FirstOrDefault() == categoryId);

        var totalItems = await query.CountAsync(ct);
        var transactions = await query
            .Include(t => t.BankAccount).ThenInclude(a => a!.Bank)
            .Include(t => t.Period)
            .OrderByDescending(t => t.TransactionDate).ThenBy(t => t.Id)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .ToListAsync(ct);

        var transactionIds = transactions.Select(t => t.Id).ToList();
        var latestClassificationByTransaction = (await db.TransactionClassifications
                .Where(c => transactionIds.Contains(c.TransactionId))
                .Include(c => c.Category)
                .ToListAsync(ct))
            .GroupBy(c => c.TransactionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedAt).First());

        var items = transactions.Select(t =>
        {
            latestClassificationByTransaction.TryGetValue(t.Id, out var latest);
            var isClassified = latest is not null && latest.Source != ClassificationSource.Unclassified;
            return new TransactionSummary(
                t.Id,
                t.BankAccountId,
                t.BankAccount?.Bank?.Name ?? "Desconocido",
                t.BankAccount?.Code ?? "Desconocido",
                t.PeriodId,
                t.Period?.Label,
                t.TransactionDate,
                t.Description,
                t.Place,
                t.CurrencyCode,
                t.Amount,
                t.AmountCrc,
                t.OperationType,
                isClassified ? "classified" : "unclassified",
                isClassified ? latest!.Category?.Name : null);
        }).ToList();

        return new PagedResult<TransactionSummary>(items, page, itemsPerPage, totalItems);
    }

    public async Task<TransactionDetail?> GetDetailAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await db.Transactions
            .Include(t => t.BankAccount).ThenInclude(a => a!.Bank)
            .Include(t => t.Period)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);
        if (transaction is null)
            return null;

        var classifications = await db.TransactionClassifications
            .Where(c => c.TransactionId == transactionId)
            .Include(c => c.Category)
            .Include(c => c.ClassificationRule)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new TransactionClassificationEntry(
                c.Id,
                c.Source,
                c.Category != null ? c.Category.Code : null,
                c.Category != null ? c.Category.Name : null,
                c.Confidence,
                c.Explanation,
                c.CreatedAt,
                c.ClassificationRule != null ? c.ClassificationRule.DescriptionPattern : null))
            .ToListAsync(ct);

        return new TransactionDetail(
            transaction.Id,
            transaction.BankAccountId,
            transaction.BankAccount?.Bank?.Name ?? "Desconocido",
            transaction.BankAccount?.Code ?? "Desconocido",
            transaction.PeriodId,
            transaction.Period?.Label,
            transaction.ReferenceNumber,
            transaction.TransactionDate,
            transaction.PaymentDate,
            transaction.Description,
            transaction.Place,
            transaction.CurrencyCode,
            transaction.Amount,
            transaction.AmountCrc,
            transaction.ExchangeRate,
            transaction.OperationType,
            classifications);
    }
}
