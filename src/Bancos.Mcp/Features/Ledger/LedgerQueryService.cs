using Bancos.Mcp.Data;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.Ledger;

public sealed record LedgerLine(
    Guid TransactionId,
    string BankName,
    string AccountCode,
    DateOnly TransactionDate,
    string? ReferenceNumber,
    string Description,
    string CurrencyCode,
    decimal Amount,
    decimal AmountCrc,
    string OperationType);

public sealed record LedgerVoucher(
    Guid VoucherId,
    DateOnly VoucherDate,
    string? ReferenceNumber,
    string Description,
    IReadOnlyList<LedgerLine> Lines);

public sealed record LedgerPeriodResult(
    Guid PeriodId,
    string PeriodLabel,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<LedgerVoucher> Vouchers,
    IReadOnlyList<string> Warnings);

public sealed class LedgerQueryService(McpCatalogDbContext db)
{
    public async Task<LedgerPeriodResult?> GetPeriodAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await db.Periods.FirstOrDefaultAsync(candidate => candidate.Id == periodId, ct);
        if (period is null)
            return null;

        var transactions = await db.Transactions
            .Where(transaction => transaction.PeriodId == periodId)
            .Include(transaction => transaction.BankAccount)
                .ThenInclude(account => account!.Bank)
            .OrderBy(transaction => transaction.TransactionDate)
            .ThenBy(transaction => transaction.Id)
            .ToListAsync(ct);

        var vouchers = transactions.Select(transaction => new LedgerVoucher(
            transaction.Id,
            transaction.TransactionDate,
            transaction.ReferenceNumber,
            transaction.Description,
            [new LedgerLine(
                transaction.Id,
                transaction.BankAccount?.Bank?.Name ?? "Desconocido",
                transaction.BankAccount?.Code ?? "Desconocido",
                transaction.TransactionDate,
                transaction.ReferenceNumber,
                transaction.Description,
                transaction.CurrencyCode,
                transaction.Amount,
                transaction.AmountCrc,
                transaction.OperationType)]))
            .ToList();

        var warnings = new List<string>();
        if (transactions.Count == 0)
            warnings.Add("El período no contiene movimientos registrados.");
        if (transactions.Any(transaction => transaction.CurrencyCode == "USD" && transaction.ExchangeRate is null))
            warnings.Add("Hay movimientos USD sin tipo de cambio registrado.");

        return new LedgerPeriodResult(
            period.Id,
            period.Label,
            period.StartDate,
            period.EndDate,
            vouchers,
            warnings);
    }
}