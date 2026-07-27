using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.Classification;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class ClassificationServiceTests
{
    private static readonly Guid GroceriesCategoryId = Guid.Parse("70000000-0000-0000-0000-000000000008");
    private static readonly Guid TransportCategoryId = Guid.Parse("70000000-0000-0000-0000-000000000009");

    [Fact]
    public async Task Classifies_by_rule_when_description_matches_and_records_origin_and_confidence()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        db.ClassificationRules.Add(new ClassificationRule
        {
            Id = Guid.NewGuid(),
            CategoryId = GroceriesCategoryId,
            DescriptionPattern = "AUTOMERCADO",
            MatchType = "contains"
        });
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA AUTOMERCADO SABANA");
        await db.SaveChangesAsync();

        var service = new ClassificationService(db);
        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal("rule", classification.Source);
        Assert.Equal(GroceriesCategoryId, classification.CategoryId);
        Assert.Equal(1m, classification.Confidence);
        Assert.NotNull(classification.ClassificationRuleId);
        Assert.NotEmpty(classification.Explanation!);
    }

    [Fact]
    public async Task Leaves_unclassified_with_no_category_when_no_rule_matches()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA CONOCIDA");
        await db.SaveChangesAsync();

        var service = new ClassificationService(db);
        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal("unclassified", classification.Source);
        Assert.Null(classification.CategoryId);
        Assert.Null(classification.ClassificationRuleId);
        Assert.Null(classification.Confidence);
    }

    [Fact]
    public async Task Prefers_the_more_specific_rule_when_several_match()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        db.ClassificationRules.AddRange(
            new ClassificationRule
            {
                Id = Guid.NewGuid(),
                CategoryId = TransportCategoryId,
                DescriptionPattern = "UBER",
                MatchType = "contains"
            },
            new ClassificationRule
            {
                Id = Guid.NewGuid(),
                CategoryId = GroceriesCategoryId,
                BankAccountId = accountId,
                DescriptionPattern = "UBER",
                MatchType = "contains"
            });
        var transactionId = await AddTransactionAsync(db, accountId, "UBER TRIP 123");
        await db.SaveChangesAsync();

        var service = new ClassificationService(db);
        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal(GroceriesCategoryId, classification.CategoryId);
    }

    [Fact]
    public async Task Ignores_disabled_rules()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        db.ClassificationRules.Add(new ClassificationRule
        {
            Id = Guid.NewGuid(),
            CategoryId = GroceriesCategoryId,
            DescriptionPattern = "AUTOMERCADO",
            MatchType = "contains",
            IsEnabled = false
        });
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA AUTOMERCADO SABANA");
        await db.SaveChangesAsync();

        var service = new ClassificationService(db);
        var classification = await service.ClassifyAsync(transactionId);

        Assert.Equal("unclassified", classification.Source);
    }

    [Fact]
    public async Task Keeps_history_of_every_classification_attempt()
    {
        await using var db = await CreateDbAsync();
        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        var transactionId = await AddTransactionAsync(db, accountId, "COMPRA SIN REGLA");
        await db.SaveChangesAsync();
        var service = new ClassificationService(db);

        await service.ClassifyAsync(transactionId);
        db.ClassificationRules.Add(new ClassificationRule
        {
            Id = Guid.NewGuid(),
            CategoryId = GroceriesCategoryId,
            DescriptionPattern = "COMPRA SIN REGLA",
            MatchType = "exact"
        });
        await db.SaveChangesAsync();
        await service.ClassifyAsync(transactionId);

        var history = await db.TransactionClassifications
            .Where(tc => tc.TransactionId == transactionId)
            .ToListAsync();

        Assert.Equal(2, history.Count);
        Assert.Contains(history, tc => tc.Source == "unclassified");
        Assert.Contains(history, tc => tc.Source == "rule" && tc.CategoryId == GroceriesCategoryId);
    }

    private static async Task<McpCatalogDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task<Guid> AddTransactionAsync(McpCatalogDbContext db, Guid accountId, string description)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = accountId,
            TransactionDate = new DateOnly(2026, 7, 20),
            Description = description,
            CurrencyCode = "CRC",
            Amount = -10m,
            AmountCrc = -10m,
            OperationType = "purchase",
            SourceFingerprint = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64]
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return transaction.Id;
    }
}
