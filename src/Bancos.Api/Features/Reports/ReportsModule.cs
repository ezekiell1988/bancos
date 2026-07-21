using Bancos.Api.Data;
using Bancos.Api.Domain;
using Hangfire;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Api.Features.Reports;
public static class ReportsModule
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services) => services.AddScoped<RegenerateReportsJob>();
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");
        group.MapPost("/regenerate", async (IBackgroundJobClient jobs, BancosDbContext db, CancellationToken ct) => { var periods = await db.ReportPeriods.ToListAsync(ct); foreach (var period in periods) period.IsStale = true; await db.SaveChangesAsync(ct); var id = jobs.Enqueue<RegenerateReportsJob>(x => x.RunAsync(null!, CancellationToken.None)); return TypedResults.Accepted($"/hangfire/jobs/details/{id}", new { jobId = id }); });
        group.MapGet("/income-statement", GetIncomeStatement);
        group.MapGet("/balance-sheet", GetBalanceSheet);
        return app;
    }

    private static async Task<Ok<IncomeStatementResponse>> GetIncomeStatement(int year, int month, BancosDbContext db, CancellationToken ct)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1);

        var rows = await db.Transactions
            .AsNoTracking()
            .Where(t => t.BookingDate >= from && t.BookingDate < to)
            .Join(db.AccountAuxiliaries, t => t.AccountAuxiliaryId, a => a.Id, (t, a) => new { t, a })
            .Join(db.Accounts, x => x.a.AccountId, acc => acc.Id, (x, acc) => new { x.t, acc.Kind })
            .Where(x => x.Kind == AccountKind.Income || x.Kind == AccountKind.Expense)
            .GroupJoin(db.Categories, x => x.t.CategoryId, c => c.Id, (x, cats) => new { x.t, x.Kind, Category = cats.FirstOrDefault() })
            .Select(x => new { x.Kind, CategoryName = x.Category != null ? x.Category.Name : "Sin categoría", x.t.AmountCrc })
            .ToListAsync(ct);

        var income = rows
            .Where(r => r.Kind == AccountKind.Income)
            .GroupBy(r => r.CategoryName)
            .Select(g => new CategoryTotal(g.Key, g.Sum(r => Math.Abs(r.AmountCrc))))
            .OrderByDescending(x => x.Total)
            .ToList();

        var expenses = rows
            .Where(r => r.Kind == AccountKind.Expense)
            .GroupBy(r => r.CategoryName)
            .Select(g => new CategoryTotal(g.Key, g.Sum(r => Math.Abs(r.AmountCrc))))
            .OrderByDescending(x => x.Total)
            .ToList();

        var totalIncome = income.Sum(x => x.Total);
        var totalExpenses = expenses.Sum(x => x.Total);

        return TypedResults.Ok(new IncomeStatementResponse(year, month, income, expenses, totalIncome, totalExpenses, totalIncome - totalExpenses));
    }

    private static async Task<Ok<BalanceSheetResponse>> GetBalanceSheet(BancosDbContext db, CancellationToken ct)
    {
        var assets = await db.Transactions
            .AsNoTracking()
            .Join(db.AccountAuxiliaries, t => t.AccountAuxiliaryId, a => a.Id, (t, a) => new { t, a })
            .Join(db.Accounts, x => x.a.AccountId, acc => acc.Id, (x, acc) => new { x.t, x.a, acc })
            .Where(x => x.acc.Kind == AccountKind.Asset)
            .GroupBy(x => new { x.a.Id, x.a.Name })
            .Select(g => new BalanceSheetLine(g.Key.Name, g.Sum(x => x.t.AmountCrc)))
            .ToListAsync(ct);

        var allCardStatements = await db.CardStatements
            .AsNoTracking()
            .Include(cs => cs.AccountAuxiliary)
            .ToListAsync(ct);
        var cardStatements = allCardStatements
            .GroupBy(cs => cs.AccountAuxiliaryId)
            .Select(g => { var latest = g.OrderByDescending(cs => cs.StatementDate).First(); return new BalanceSheetLine(latest.AccountAuxiliary!.Name + " (" + latest.CardNumberMasked + ")", latest.CashPaymentCrc); })
            .ToList();

        var allFinancings = await db.CreditFinancings
            .AsNoTracking()
            .Include(cf => cf.AccountAuxiliary)
            .ToListAsync(ct);
        var financings = allFinancings
            .GroupBy(cf => cf.AccountAuxiliaryId)
            .Select(g => new BalanceSheetLine(g.First().AccountAuxiliary!.Name + " (financiamientos)", g.Sum(cf => cf.OutstandingBalance)))
            .ToList();

        var allLoans = await db.LoanStatements
            .AsNoTracking()
            .Include(ls => ls.AccountAuxiliary)
            .ToListAsync(ct);
        var loans = allLoans
            .GroupBy(ls => ls.AccountAuxiliaryId)
            .Select(g => { var latest = g.OrderByDescending(ls => ls.CreatedUtc).First(); return new BalanceSheetLine(latest.AccountAuxiliary!.Name + " (préstamo)", latest.OutstandingBalance); })
            .ToList();

        var liabilities = cardStatements.Concat(financings).Concat(loans).ToList();
        var totalAssets = assets.Sum(x => x.AmountCrc);
        var totalLiabilities = liabilities.Sum(x => x.AmountCrc);

        return TypedResults.Ok(new BalanceSheetResponse(assets, liabilities, totalAssets, totalLiabilities, totalAssets - totalLiabilities));
    }
}

public sealed record CategoryTotal(string Category, decimal Total);
public sealed record IncomeStatementResponse(int Year, int Month, List<CategoryTotal> Income, List<CategoryTotal> Expenses, decimal TotalIncome, decimal TotalExpenses, decimal NetResult);
public sealed record BalanceSheetLine(string Name, decimal AmountCrc);
public sealed record BalanceSheetResponse(List<BalanceSheetLine> Assets, List<BalanceSheetLine> Liabilities, decimal TotalAssets, decimal TotalLiabilities, decimal NetWorth);
