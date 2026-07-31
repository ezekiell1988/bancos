using Bancos.Mcp.Data;
using Bancos.Mcp.Features.ExchangeRates;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.ForeignExchange;

public sealed record ForeignExchangeLine(
    Guid BankAccountId,
    string BankName,
    string AccountCode,
    decimal OpeningBalanceUsd,
    decimal PeriodMovementUsd,
    decimal ClosingBalanceUsd,
    decimal? PreviousRate,
    decimal? ClosingRate,
    decimal? DifferenceCrc,
    IReadOnlyList<Guid> DocumentIds);

public sealed record ForeignExchangeClosing(
    Guid PeriodId,
    string PeriodLabel,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Status,
    decimal TotalDifferenceCrc,
    IReadOnlyList<ForeignExchangeLine> Lines,
    IReadOnlyList<string> Warnings);

public sealed class ForeignExchangeService(McpCatalogDbContext db, ExchangeRateService exchangeRateService)
{
    public async Task<ForeignExchangeClosing?> CalculateAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await db.Periods.FirstOrDefaultAsync(candidate => candidate.Id == periodId, ct);
        if (period is null)
            return null;

        var transactions = await db.Transactions
            .Where(transaction => transaction.CurrencyCode == "USD"
                && transaction.TransactionDate <= period.EndDate
                && (transaction.BankAccount!.AccountType == "credit-card"
                    || transaction.BankAccount.AccountType == "loan"))
            .Include(transaction => transaction.BankAccount)
                .ThenInclude(account => account!.Bank)
            .OrderBy(transaction => transaction.TransactionDate)
            .ThenBy(transaction => transaction.Id)
            .ToListAsync(ct);

        var lines = new List<ForeignExchangeLine>();
        var warnings = new List<string>();
        foreach (var accountTransactions in transactions.GroupBy(transaction => transaction.BankAccountId))
        {
            var first = accountTransactions.First();
            var openingTransactions = accountTransactions
                .Where(transaction => transaction.TransactionDate < period.StartDate)
                .ToList();
            var periodTransactions = accountTransactions
                .Where(transaction => transaction.PeriodId == period.Id)
                .ToList();
            var openingBalanceUsd = openingTransactions.Sum(transaction => transaction.Amount);
            var periodMovementUsd = periodTransactions.Sum(transaction => transaction.Amount);
            var closingBalanceUsd = openingBalanceUsd + periodMovementUsd;
            if (openingBalanceUsd == 0m && closingBalanceUsd == 0m)
                continue;

            var bankCode = first.BankAccount?.Bank?.Code;
            var previousRate = await exchangeRateService.ResolveAsync(
                period.StartDate.AddDays(-1), "USD", bankCode, ct);
            var closingRate = await exchangeRateService.ResolveAsync(
                period.EndDate, "USD", bankCode, ct);

            if (!previousRate.Found)
                warnings.Add($"No existe tipo de cambio inicial para la cuenta {first.BankAccount?.Code ?? first.BankAccountId.ToString()}.");
            else if (previousRate.IsFallback)
                warnings.Add($"La cuenta {first.BankAccount?.Code ?? first.BankAccountId.ToString()} usa tipo de cambio inicial previo al período.");
            if (!closingRate.Found)
                warnings.Add($"No existe tipo de cambio de cierre para la cuenta {first.BankAccount?.Code ?? first.BankAccountId.ToString()}.");
            else if (closingRate.IsFallback)
                warnings.Add($"La cuenta {first.BankAccount?.Code ?? first.BankAccountId.ToString()} usa tipo de cambio de cierre previo al período.");

            decimal? difference = null;
            if (previousRate.CrcPerUnit is { } openingRate && closingRate.CrcPerUnit is { } endingRate)
                difference = decimal.Round(openingBalanceUsd * (endingRate - openingRate), 2);

            lines.Add(new ForeignExchangeLine(
                first.BankAccountId,
                first.BankAccount?.Bank?.Name ?? "Desconocido",
                first.BankAccount?.Code ?? "Desconocido",
                openingBalanceUsd,
                periodMovementUsd,
                closingBalanceUsd,
                previousRate.CrcPerUnit,
                closingRate.CrcPerUnit,
                difference,
                periodTransactions.Select(transaction => transaction.Id).ToList()));
        }

        if (lines.Count == 0)
            warnings.Add("No hay saldos o movimientos USD de pasivos para este período.");

        var status = warnings.Count == 0 ? "completed" : "completed_with_warnings";
        return new ForeignExchangeClosing(
            period.Id,
            period.Label,
            period.StartDate,
            period.EndDate,
            status,
            lines.Sum(line => line.DifferenceCrc ?? 0m),
            lines,
            warnings);
    }
}