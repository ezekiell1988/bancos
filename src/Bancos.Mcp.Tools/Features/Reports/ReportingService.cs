using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.Reports;

public sealed record CategoryAmount(string CategoryCode, string CategoryName, decimal AmountCrc);

public sealed record IncomeStatementReport(
    string PeriodLabel,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<CategoryAmount> IncomeLines,
    IReadOnlyList<CategoryAmount> ExpenseLines,
    decimal TotalIncome,
    decimal TotalExpense,
    int PendingClassificationCount)
{
    public Guid PeriodId { get; init; }
    public IncomeStatementReport? PreviousPeriod { get; init; }
    public decimal NetResult => TotalIncome - TotalExpense;
}

public sealed record BalanceSheetAccountAmount(string BankName, string AccountCode, decimal AmountCrc);

public sealed record BalanceSheetReport(
    string PeriodLabel,
    DateOnly AsOfDate,
    IReadOnlyList<BalanceSheetAccountAmount> AssetLines,
    IReadOnlyList<BalanceSheetAccountAmount> LiabilityLines,
    decimal TotalAssets,
    decimal TotalLiabilities,
    int AccountsMissingClosingCount)
{
    public Guid PeriodId { get; init; }
    public BalanceSheetReport? PreviousPeriod { get; init; }
    public decimal Equity => TotalAssets - TotalLiabilities;
    public decimal BalanceDifference => TotalAssets - TotalLiabilities - Equity;
}

public sealed class ReportingService(McpCatalogDbContext db)
{
    public async Task<IncomeStatementReport> GetIncomeStatementAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await db.Periods.FindAsync([periodId], ct)
            ?? throw new InvalidOperationException($"Período {periodId} no encontrado.");

        var report = await BuildIncomeStatementAsync(period, ct);
        var previousPeriod = await FindPreviousPeriodAsync(period, ct);
        if (previousPeriod is null)
            return report;

        return report with { PreviousPeriod = await BuildIncomeStatementAsync(previousPeriod, ct) };
    }

    public async Task<BalanceSheetReport> GetBalanceSheetAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await db.Periods.FindAsync([periodId], ct)
            ?? throw new InvalidOperationException($"Período {periodId} no encontrado.");

        var report = await BuildBalanceSheetAsync(period, ct);
        var previousPeriod = await FindPreviousPeriodAsync(period, ct);
        if (previousPeriod is null)
            return report;

        return report with { PreviousPeriod = await BuildBalanceSheetAsync(previousPeriod, ct) };
    }

    private async Task<IncomeStatementReport> BuildIncomeStatementAsync(Period period, CancellationToken ct)
    {

        var transactions = await db.Transactions
            .Where(t => t.PeriodId == period.Id)
            .Select(t => new { t.Id, t.AmountCrc })
            .ToListAsync(ct);
        var transactionIds = transactions.Select(t => t.Id).ToList();

        var latestClassificationByTransaction = (await db.TransactionClassifications
                .Where(c => transactionIds.Contains(c.TransactionId))
                .Select(c => new { c.TransactionId, c.CategoryId, c.Source, c.CreatedAt })
                .ToListAsync(ct))
            .GroupBy(c => c.TransactionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedAt).First());

        var categories = await db.Categories.ToDictionaryAsync(c => c.Id, ct);

        var incomeTotals = new Dictionary<Guid, decimal>();
        var expenseTotals = new Dictionary<Guid, decimal>();
        var pending = 0;

        foreach (var transaction in transactions)
        {
            if (!latestClassificationByTransaction.TryGetValue(transaction.Id, out var classification)
                || classification.CategoryId is null
                || classification.Source == "unclassified"
                || !categories.TryGetValue(classification.CategoryId.Value, out var category))
            {
                pending++;
                continue;
            }

            // En los datos importados, un depósito/ingreso llega con amountCrc positivo y una
            // compra/gasto con amountCrc negativo (mismo signo que usa el saldo acumulado de
            // AccountPeriodClosing). Se invierte el signo del gasto para mostrar ambas magnitudes
            // como valores positivos en el reporte.
            switch (category.RootType)
            {
                case "expense":
                    expenseTotals[category.Id] = expenseTotals.GetValueOrDefault(category.Id) - transaction.AmountCrc;
                    break;
                case "income":
                    incomeTotals[category.Id] = incomeTotals.GetValueOrDefault(category.Id) + transaction.AmountCrc;
                    break;
            }
        }

        List<CategoryAmount> ToLines(Dictionary<Guid, decimal> totals) => totals
            .Select(kvp => new CategoryAmount(categories[kvp.Key].Code, categories[kvp.Key].Name, kvp.Value))
            .OrderByDescending(line => line.AmountCrc)
            .ToList();

        var incomeLines = ToLines(incomeTotals);
        var expenseLines = ToLines(expenseTotals);

        return new IncomeStatementReport(
            period.Label,
            period.StartDate,
            period.EndDate,
            incomeLines,
            expenseLines,
            incomeLines.Sum(line => line.AmountCrc),
            expenseLines.Sum(line => line.AmountCrc),
            pending)
        {
            PeriodId = period.Id
        };
    }

    private async Task<BalanceSheetReport> BuildBalanceSheetAsync(Period period, CancellationToken ct)
    {
        var closings = await db.AccountPeriodClosings
            .Where(c => c.PeriodId == period.Id)
            .Include(c => c.BankAccount)
                .ThenInclude(a => a!.Bank)
            .ToListAsync(ct);

        var assetLines = new List<BalanceSheetAccountAmount>();
        var liabilityLines = new List<BalanceSheetAccountAmount>();

        foreach (var closing in closings)
        {
            var bankName = closing.BankAccount?.Bank?.Name ?? "Desconocido";
            var accountCode = closing.BankAccount?.Code ?? "Desconocido";

            // Saldo acumulado positivo = activo (hay dinero); negativo = pasivo (se debe).
            // Se muestra como magnitud positiva en ambos casos.
            if (closing.Balance >= 0)
                assetLines.Add(new BalanceSheetAccountAmount(bankName, accountCode, closing.Balance));
            else
                liabilityLines.Add(new BalanceSheetAccountAmount(bankName, accountCode, -closing.Balance));
        }

        var accountsWithActivity = await db.Transactions
            .Where(t => t.TransactionDate <= period.EndDate)
            .Select(t => t.BankAccountId)
            .Distinct()
            .ToListAsync(ct);
        var accountsWithClosing = closings.Select(c => c.BankAccountId).ToHashSet();
        var missingClosingCount = accountsWithActivity.Count(id => !accountsWithClosing.Contains(id));

        return new BalanceSheetReport(
            period.Label,
            period.EndDate,
            assetLines.OrderByDescending(line => line.AmountCrc).ToList(),
            liabilityLines.OrderByDescending(line => line.AmountCrc).ToList(),
            assetLines.Sum(line => line.AmountCrc),
            liabilityLines.Sum(line => line.AmountCrc),
            missingClosingCount)
        {
            PeriodId = period.Id
        };
    }

    private Task<Period?> FindPreviousPeriodAsync(Period period, CancellationToken ct) => db.Periods
        .Where(candidate => candidate.StartDate < period.StartDate)
        .OrderByDescending(candidate => candidate.StartDate)
        .FirstOrDefaultAsync(ct);
}
