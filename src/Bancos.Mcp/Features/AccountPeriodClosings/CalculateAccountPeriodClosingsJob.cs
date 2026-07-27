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
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task ExecuteAsync(Guid periodId, PerformContext? context)
    {
        logger.LogInformation("Calculando cierres desde periodo {PeriodId}", periodId);

        var allPeriods = await db.Periods
            .Where(p => p.StartDate >= db.Periods.Where(x => x.Id == periodId).Select(x => x.StartDate).First())
            .OrderBy(p => p.StartDate)
            .ToListAsync();

        if (allPeriods.Count == 0)
        {
            context?.WriteLine("No se encontró el periodo {0}.", periodId);
            return;
        }

        context?.WriteLine("Periodos a procesar: {0}", allPeriods.Count);

        var periodIds = allPeriods.Select(p => p.Id).ToList();
        var accountIds = await db.Transactions
            .Where(t => t.PeriodId != null && periodIds.Contains(t.PeriodId!.Value))
            .Select(t => t.BankAccountId)
            .Distinct()
            .ToListAsync();

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
                .Select(x => x.Balance)
                .FirstOrDefaultAsync();

            foreach (var period in allPeriods)
            {
                var movements = await db.Transactions
                    .Where(t => t.BankAccountId == accountId && t.PeriodId == period.Id)
                    .SumAsync(t => (decimal?)t.AmountCrc) ?? 0m;

                var balance = previousBalance + movements;

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
}
