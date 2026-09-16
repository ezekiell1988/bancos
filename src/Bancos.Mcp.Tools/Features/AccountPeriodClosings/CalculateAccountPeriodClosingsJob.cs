using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.AccountPeriodClosings;

[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
public sealed class CalculateAccountPeriodClosingsJob(McpCatalogDbContext db, ILogger<CalculateAccountPeriodClosingsJob> logger)
{
    private static readonly string[] MonthLabels = ["ENE", "FEB", "MAR", "ABR", "MAY", "JUN", "JUL", "AGO", "SEP", "OCT", "NOV", "DIC"];

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task ExecuteAsync(Guid periodId, PerformContext? context)
    {
        logger.LogInformation("Calculando cierres desde periodo {PeriodId}", periodId);

        var requestedPeriod = await db.Periods.SingleOrDefaultAsync(p => p.Id == periodId);
        if (requestedPeriod is null)
        {
            context?.WriteLine("No se encontró el periodo solicitado.");
            return;
        }

        var earliestMovementPeriodStart = await EnsurePeriodsAndAssignTransactionsAsync();
        if (earliestMovementPeriodStart is null)
        {
            context?.WriteLine("No hay movimientos para calcular cierres.");
            return;
        }

        var closingStartDate = requestedPeriod.StartDate;
        var allPeriods = await db.Periods
            .Where(p => p.StartDate >= closingStartDate)
            .OrderBy(p => p.StartDate)
            .ToListAsync();

        context?.WriteLine("Periodos a procesar: {0}", allPeriods.Count);

        var periodIds = allPeriods.Select(p => p.Id).ToList();
        var accountsWithMovements = await db.Transactions
            .Where(t => t.PeriodId != null && periodIds.Contains(t.PeriodId!.Value))
            .Select(t => t.BankAccountId)
            .Distinct()
            .ToListAsync();
        var accountsWithClosings = await db.AccountPeriodClosings
            .Select(c => c.BankAccountId)
            .Distinct()
            .ToListAsync();
        var accountIds = accountsWithMovements
            .Concat(accountsWithClosings)
            .Distinct()
            .ToList();

        context?.WriteLine("Cuentas con movimientos: {0}", accountIds.Count);

        var processed = 0;
        foreach (var accountId in accountIds)
        {
            var previousPeriodStart = allPeriods[0].StartDate;
            var previousBalance = await db.AccountPeriodClosings
                .Where(c => c.BankAccountId == accountId)
                .Join(db.Periods, c => c.PeriodId, p => p.Id, (c, p) => new { c.Balance, p.StartDate })
                .Where(x => x.StartDate < previousPeriodStart)
                .OrderByDescending(x => x.StartDate)
                .Select(x => (decimal?)x.Balance)
                .FirstOrDefaultAsync();

            previousBalance ??= await db.Transactions
                .Where(t => t.BankAccountId == accountId && t.TransactionDate < previousPeriodStart)
                .SumAsync(t => (decimal?)t.AmountCrc) ?? 0m;

            foreach (var period in allPeriods)
            {
                var movements = await db.Transactions
                    .Where(t => t.BankAccountId == accountId && t.PeriodId == period.Id)
                    .SumAsync(t => (decimal?)t.AmountCrc) ?? 0m;

                var balance = previousBalance.Value + movements;

                var existing = await db.AccountPeriodClosings
                    .FirstOrDefaultAsync(c => c.BankAccountId == accountId && c.PeriodId == period.Id);

                if (existing is not null)
                {
                    existing.Balance = balance;
                    existing.UpdatedAt = CostaRicaTime.Now;
                }
                else
                {
                    db.AccountPeriodClosings.Add(new AccountPeriodClosing
                    {
                        BankAccountId = accountId,
                        PeriodId = period.Id,
                        Balance = balance
                    });
                }

                previousBalance = balance;
            }

            await db.SaveChangesAsync();
            processed++;
        }

        context?.WriteLine("Cierres calculados para {0} cuenta(s).", processed);
        logger.LogInformation("Cierres calculados para {Count} cuenta(s) desde periodo {PeriodId}", processed, periodId);
    }

    private async Task<DateOnly?> EnsurePeriodsAndAssignTransactionsAsync()
    {
        var transactionDates = await db.Transactions
            .Select(transaction => transaction.TransactionDate)
            .ToListAsync();
        if (transactionDates.Count == 0) return null;

        var firstPeriodStart = GetPeriodStart(transactionDates.Min());
        var lastPeriodStart = GetPeriodStart(transactionDates.Max());
        var existingStarts = await db.Periods
            .Select(period => period.StartDate)
            .ToHashSetAsync();

        for (var start = firstPeriodStart; start <= lastPeriodStart; start = start.AddMonths(1))
        {
            if (existingStarts.Contains(start)) continue;
            var end = start.AddMonths(1).AddDays(-1);
            db.Periods.Add(new Period
            {
                Id = Guid.NewGuid(),
                Label = $"{MonthLabels[end.Month - 1]}-{end.Year}",
                StartDate = start,
                EndDate = end
            });
        }
        await db.SaveChangesAsync();

        var periods = await db.Periods
            .OrderBy(period => period.StartDate)
            .ToListAsync();
        var transactions = await db.Transactions.ToListAsync();
        var assigned = 0;
        foreach (var transaction in transactions)
        {
            var period = periods.First(period =>
                period.StartDate <= transaction.TransactionDate && transaction.TransactionDate <= period.EndDate);
            if (transaction.PeriodId == period.Id) continue;
            transaction.PeriodId = period.Id;
            transaction.UpdatedAt = CostaRicaTime.Now;
            assigned++;
        }
        if (assigned > 0) await db.SaveChangesAsync();

        return firstPeriodStart;
    }

    private static DateOnly GetPeriodStart(DateOnly date) => date.Day >= 19
        ? new DateOnly(date.Year, date.Month, 19)
        : new DateOnly(date.Year, date.Month, 1).AddMonths(-1).AddDays(18);
}
