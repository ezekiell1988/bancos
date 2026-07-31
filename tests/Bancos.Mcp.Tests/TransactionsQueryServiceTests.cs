using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.Transactions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class TransactionsQueryServiceTests
{
    private static readonly Guid AccountOneId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid AccountTwoId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid JulPeriodId = Guid.Parse("60000000-0000-0000-0000-000000000007"); // JUL-2026
    private static readonly Guid AugPeriodId = Guid.Parse("60000000-0000-0000-0000-000000000008"); // AGO-2026
    private static readonly Guid GroceriesCategoryId = Guid.Parse("70000000-0000-0000-0000-000000000008"); // expense.groceries
    private static readonly Guid TransportCategoryId = Guid.Parse("70000000-0000-0000-0000-000000000009"); // expense.transport

    private static async Task<McpCatalogDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static Transaction NewTransaction(Guid accountId, Guid? periodId, DateOnly date, decimal amount = -10m) => new()
    {
        Id = Guid.NewGuid(),
        BankAccountId = accountId,
        PeriodId = periodId,
        TransactionDate = date,
        Description = "Movimiento de prueba",
        CurrencyCode = "CRC",
        Amount = amount,
        AmountCrc = amount,
        OperationType = "purchase",
        SourceFingerprint = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64]
    };

    [Fact]
    public async Task Filters_by_bank_account_and_period()
    {
        await using var db = await CreateDbAsync();
        var matching = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 10));
        var otherAccount = NewTransaction(AccountTwoId, JulPeriodId, new DateOnly(2026, 7, 11));
        var otherPeriod = NewTransaction(AccountOneId, AugPeriodId, new DateOnly(2026, 8, 1));
        db.Transactions.AddRange(matching, otherAccount, otherPeriod);
        await db.SaveChangesAsync();

        var service = new TransactionsQueryService(db);
        var result = await service.SearchAsync(AccountOneId, JulPeriodId, null, null, null, null, 1, 50);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal(matching.Id, result.Items.Single().TransactionId);
    }

    [Fact]
    public async Task Filters_by_date_range()
    {
        await using var db = await CreateDbAsync();
        var inRange = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 10));
        var beforeRange = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 1));
        var afterRange = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 20));
        db.Transactions.AddRange(inRange, beforeRange, afterRange);
        await db.SaveChangesAsync();

        var service = new TransactionsQueryService(db);
        var result = await service.SearchAsync(null, null, null, null, new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 15), 1, 50);

        Assert.Equal(inRange.Id, result.Items.Single().TransactionId);
    }

    [Fact]
    public async Task Filters_by_classification_status()
    {
        await using var db = await CreateDbAsync();
        var unclassified = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 10));
        var classified = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 11));
        db.Transactions.AddRange(unclassified, classified);
        db.TransactionClassifications.Add(new TransactionClassification
        {
            Id = Guid.NewGuid(),
            TransactionId = classified.Id,
            CategoryId = GroceriesCategoryId,
            Source = "manual",
            Confidence = 1m,
            Explanation = "Confirmado manualmente."
        });
        await db.SaveChangesAsync();

        var service = new TransactionsQueryService(db);
        var unclassifiedResult = await service.SearchAsync(null, JulPeriodId, null, "unclassified", null, null, 1, 50);
        var classifiedResult = await service.SearchAsync(null, JulPeriodId, null, "classified", null, null, 1, 50);

        Assert.Equal(unclassified.Id, unclassifiedResult.Items.Single().TransactionId);
        Assert.Equal(classified.Id, classifiedResult.Items.Single().TransactionId);
        Assert.Equal("Alimentación", classifiedResult.Items.Single().CategoryName);
        Assert.Null(unclassifiedResult.Items.Single().CategoryName);
    }

    [Fact]
    public async Task Filters_by_category_using_the_most_recent_classification()
    {
        await using var db = await CreateDbAsync();
        var groceries = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 10));
        var reclassifiedAwayFromGroceries = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 11));
        var unrelated = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 12));
        db.Transactions.AddRange(groceries, reclassifiedAwayFromGroceries, unrelated);

        db.TransactionClassifications.Add(new TransactionClassification
        {
            Id = Guid.NewGuid(),
            TransactionId = groceries.Id,
            CategoryId = GroceriesCategoryId,
            Source = "manual",
            Confidence = 1m,
            Explanation = "Confirmado manualmente.",
            CreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-6))
        });

        // First classified as groceries, then reclassified to a different category — only the latest counts.
        db.TransactionClassifications.Add(new TransactionClassification
        {
            Id = Guid.NewGuid(),
            TransactionId = reclassifiedAwayFromGroceries.Id,
            CategoryId = GroceriesCategoryId,
            Source = "ai",
            Confidence = 0.9m,
            Explanation = "Sugerencia inicial de IA.",
            CreatedAt = new DateTimeOffset(2026, 7, 11, 8, 0, 0, TimeSpan.FromHours(-6))
        });
        db.TransactionClassifications.Add(new TransactionClassification
        {
            Id = Guid.NewGuid(),
            TransactionId = reclassifiedAwayFromGroceries.Id,
            CategoryId = TransportCategoryId,
            Source = "manual",
            Confidence = 1m,
            Explanation = "Corrección manual.",
            CreatedAt = new DateTimeOffset(2026, 7, 11, 9, 0, 0, TimeSpan.FromHours(-6))
        });
        await db.SaveChangesAsync();

        var service = new TransactionsQueryService(db);
        var result = await service.SearchAsync(null, JulPeriodId, GroceriesCategoryId, null, null, null, 1, 50);

        Assert.Equal(groceries.Id, result.Items.Single().TransactionId);
    }

    [Fact]
    public async Task Orders_results_by_date_descending_then_id_for_stable_paging()
    {
        await using var db = await CreateDbAsync();
        var older = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 1));
        var newer = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 15));
        db.Transactions.AddRange(older, newer);
        await db.SaveChangesAsync();

        var service = new TransactionsQueryService(db);
        var result = await service.SearchAsync(AccountOneId, JulPeriodId, null, null, null, null, 1, 50);

        Assert.Equal(newer.Id, result.Items[0].TransactionId);
        Assert.Equal(older.Id, result.Items[1].TransactionId);
    }

    [Fact]
    public async Task Detail_returns_full_classification_history_ordered_by_most_recent_first()
    {
        await using var db = await CreateDbAsync();
        var transaction = NewTransaction(AccountOneId, JulPeriodId, new DateOnly(2026, 7, 10));
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        db.TransactionClassifications.Add(new TransactionClassification
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            Source = "unclassified",
            Explanation = "Sin coincidencia.",
            CreatedAt = new DateTimeOffset(2026, 7, 10, 8, 0, 0, TimeSpan.FromHours(-6))
        });
        db.TransactionClassifications.Add(new TransactionClassification
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            CategoryId = GroceriesCategoryId,
            Source = "manual",
            Confidence = 1m,
            Explanation = "Confirmado manualmente.",
            CreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(-6))
        });
        await db.SaveChangesAsync();

        var service = new TransactionsQueryService(db);
        var detail = await service.GetDetailAsync(transaction.Id);

        Assert.NotNull(detail);
        Assert.Equal(2, detail!.Classifications.Count);
        Assert.Equal("manual", detail.Classifications[0].Source);
        Assert.Equal("unclassified", detail.Classifications[1].Source);
    }

    [Fact]
    public async Task Detail_returns_null_for_unknown_transaction()
    {
        await using var db = await CreateDbAsync();
        var service = new TransactionsQueryService(db);

        var detail = await service.GetDetailAsync(Guid.NewGuid());

        Assert.Null(detail);
    }
}
