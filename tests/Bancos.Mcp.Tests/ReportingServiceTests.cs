using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.Reports;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class ReportingServiceTests
{
    private static async Task<McpCatalogDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static readonly Guid PeriodId = Guid.Parse("60000000-0000-0000-0000-000000000007"); // JUL-2026
    private static readonly Guid SalaryCategoryId = Guid.Parse("70000000-0000-0000-0000-000000000006"); // income.salary
    private static readonly Guid GroceriesCategoryId = Guid.Parse("70000000-0000-0000-0000-000000000008"); // expense.groceries

    private static Transaction NewTransaction(Guid accountId, decimal amountCrc, string operationType = "purchase") => new()
    {
        Id = Guid.NewGuid(),
        BankAccountId = accountId,
        PeriodId = PeriodId,
        TransactionDate = new DateOnly(2026, 7, 1),
        Description = "Movimiento de prueba",
        CurrencyCode = "CRC",
        Amount = amountCrc,
        AmountCrc = amountCrc,
        OperationType = operationType,
        SourceFingerprint = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64]
    };

    [Fact]
    public async Task Income_statement_sums_income_and_expense_and_flags_pending()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;

        var salary = NewTransaction(accountId, 500000m, "payment");
        var groceries = NewTransaction(accountId, -45000m);
        var unclassified = NewTransaction(accountId, -12000m);
        db.Transactions.AddRange(salary, groceries, unclassified);
        db.TransactionClassifications.AddRange(
            new TransactionClassification { Id = Guid.NewGuid(), TransactionId = salary.Id, CategoryId = SalaryCategoryId, Source = "manual" },
            new TransactionClassification { Id = Guid.NewGuid(), TransactionId = groceries.Id, CategoryId = GroceriesCategoryId, Source = "rule" });
        await db.SaveChangesAsync();

        var service = new ReportingService(db);
        var report = await service.GetIncomeStatementAsync(PeriodId);

        Assert.Equal(500000m, report.TotalIncome);
        Assert.Equal(45000m, report.TotalExpense);
        Assert.Equal(455000m, report.NetResult);
        Assert.Equal(1, report.PendingClassificationCount);
        Assert.Single(report.IncomeLines);
        Assert.Single(report.ExpenseLines);
    }

    [Fact]
    public async Task Balance_sheet_splits_positive_and_negative_closings_and_capital_balances()
    {
        await using var db = await CreateDbAsync();
        var accounts = await db.BankAccounts.Take(2).ToListAsync();
        var assetAccount = accounts[0];
        var liabilityAccount = accounts[1];

        db.AccountPeriodClosings.AddRange(
            new AccountPeriodClosing { Id = Guid.NewGuid(), BankAccountId = assetAccount.Id, PeriodId = PeriodId, Balance = 100000m },
            new AccountPeriodClosing { Id = Guid.NewGuid(), BankAccountId = liabilityAccount.Id, PeriodId = PeriodId, Balance = -30000m });
        await db.SaveChangesAsync();

        var service = new ReportingService(db);
        var report = await service.GetBalanceSheetAsync(PeriodId);

        Assert.Equal(100000m, report.TotalAssets);
        Assert.Equal(30000m, report.TotalLiabilities);
        Assert.Equal(70000m, report.Equity);
        Assert.Equal(report.TotalAssets, report.TotalLiabilities + report.Equity);
    }

    [Fact]
    public void Html_renderer_escapes_category_and_account_names()
    {
        var report = new IncomeStatementReport(
            "JUL-2026",
            new DateOnly(2026, 6, 19),
            new DateOnly(2026, 7, 18),
            [new CategoryAmount("income.other", "<script>alert(1)</script>", 1000m)],
            [],
            1000m,
            0m,
            0);

        var html = ReportHtmlRenderer.RenderIncomeStatement(report, new DateTimeOffset(2026, 7, 29, 0, 0, 0, TimeSpan.Zero));

        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
