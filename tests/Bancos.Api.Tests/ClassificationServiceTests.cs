using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Features.Classification;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Api.Tests;

public sealed class ClassificationServiceTests
{
    [Fact]
    public async Task Exact_approved_classification_precedes_matching_rule()
    {
        await using var db = CreateContext();
        var exactCategory = new Category { Name = "Exact" }; var ruleCategory = new Category { Name = "Rule" };
        var accountId = Guid.NewGuid();
        db.Categories.AddRange(exactCategory, ruleCategory);
        db.Transactions.Add(new Transaction { AccountAuxiliaryId = accountId, ImportId = Guid.NewGuid(), BookingDate = new DateOnly(2026, 1, 1), ExternalReference = "previous", SourceFingerprint = "previous", AmountCrc = 1, DescriptionNormalized = "SUPERMERCADO", CategoryId = exactCategory.Id, ClassificationSource = ClassificationSource.Manual, ClassificationStatus = ClassificationStatus.Approved });
        db.ClassificationRules.Add(new ClassificationRule { AccountAuxiliaryId = accountId, Pattern = "SUPER", CategoryId = ruleCategory.Id, IsApproved = true });
        await db.SaveChangesAsync();

        var transaction = NewTransaction(accountId, "SUPERMERCADO");
        await new ClassificationService(db).ClassifyAsync(transaction);

        Assert.Equal(exactCategory.Id, transaction.CategoryId);
        Assert.Equal(ClassificationSource.ExactApproved, transaction.ClassificationSource);
        Assert.Equal(ClassificationStatus.Approved, transaction.ClassificationStatus);
    }

    [Fact]
    public async Task Approved_account_rule_is_applied_deterministically()
    {
        await using var db = CreateContext();
        var category = new Category { Name = "Transport" }; var accountId = Guid.NewGuid();
        db.Categories.Add(category); db.ClassificationRules.Add(new ClassificationRule { AccountAuxiliaryId = accountId, Pattern = "UBER", CategoryId = category.Id, IsApproved = true }); await db.SaveChangesAsync();

        var transaction = NewTransaction(accountId, "PAGO UBER COSTA RICA");
        await new ClassificationService(db).ClassifyAsync(transaction);

        Assert.Equal(category.Id, transaction.CategoryId);
        Assert.Equal(ClassificationSource.Rule, transaction.ClassificationSource);
        Assert.Equal(ClassificationStatus.Approved, transaction.ClassificationStatus);
    }

    [Fact]
    public async Task Unknown_movement_uses_general_and_requires_review()
    {
        await using var db = CreateContext();
        var transaction = NewTransaction(Guid.NewGuid(), "MOVIMIENTO DESCONOCIDO");

        await new ClassificationService(db).ClassifyAsync(transaction);

        Assert.Equal(ClassificationSource.General, transaction.ClassificationSource);
        Assert.Equal(ClassificationStatus.PendingReview, transaction.ClassificationStatus);
        Assert.NotEqual(Guid.Empty, transaction.CategoryId);
    }

    [Fact]
    public async Task Ai_can_create_a_family_category_after_deterministic_options_are_exhausted()
    {
        await using var db = CreateContext();
        var transaction = NewTransaction(Guid.NewGuid(), "COMPRA HOGAR");
        var ai = new StubCategorySuggester(new FamilyCategorySuggestion("Hogar", true, 0.95m));

        await new ClassificationService(db, ai).ClassifyAsync(transaction);

        var category = Assert.Single(db.Categories.Local);
        Assert.Equal("Hogar", category.Name);
        Assert.Equal(category.Id, transaction.CategoryId);
        Assert.Equal(ClassificationSource.Ai, transaction.ClassificationSource);
        Assert.Equal(ClassificationStatus.Approved, transaction.ClassificationStatus);
        Assert.Equal("COMPRA HOGAR", ai.Description);
    }

    [Fact]
    public async Task Deterministic_rule_does_not_call_ai()
    {
        await using var db = CreateContext();
        var category = new Category { Name = "Transporte" }; var accountId = Guid.NewGuid();
        db.Categories.Add(category); db.ClassificationRules.Add(new ClassificationRule { Pattern = "BUS", CategoryId = category.Id, IsApproved = true }); await db.SaveChangesAsync();
        var ai = new StubCategorySuggester(new FamilyCategorySuggestion("Otro", true, 1m));

        var transaction = NewTransaction(accountId, "PAGO BUS");
        await new ClassificationService(db, ai).ClassifyAsync(transaction);

        Assert.Equal(ClassificationSource.Rule, transaction.ClassificationSource);
        Assert.Null(ai.Description);
    }

    [Fact]
    public async Task Reuses_ai_category_inside_the_same_unsaved_batch()
    {
        await using var db = CreateContext();
        var accountId = Guid.NewGuid();
        var ai = new StubCategorySuggester(new FamilyCategorySuggestion("Alimentación", true, 0.95m));
        var service = new ClassificationService(db, ai);
        var first = NewTransaction(accountId, "COMPRA MERCADO");
        await service.ClassifyAsync(first); db.Transactions.Add(first);

        var second = NewTransaction(accountId, "COMPRA MERCADO");
        await service.ClassifyAsync(second); db.Transactions.Add(second);

        Assert.Single(db.Categories.Local);
        Assert.Equal(first.CategoryId, second.CategoryId);
        Assert.Equal(ClassificationSource.ExactApproved, second.ClassificationSource);
        Assert.Equal(1, ai.Calls);
    }

    private static BancosDbContext CreateContext() => new(new DbContextOptionsBuilder<BancosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Transaction NewTransaction(Guid accountId, string description) => new() { AccountAuxiliaryId = accountId, ImportId = Guid.NewGuid(), BookingDate = new DateOnly(2026, 1, 2), ExternalReference = Guid.NewGuid().ToString(), SourceFingerprint = Guid.NewGuid().ToString(), AmountCrc = 1, DescriptionNormalized = description };

    private sealed class StubCategorySuggester(FamilyCategorySuggestion? suggestion) : IFamilyCategorySuggester
    {
        public string? Description { get; private set; }
        public int Calls { get; private set; }
        public Task<FamilyCategorySuggestion?> SuggestAsync(string normalizedDescription, IReadOnlyList<string> categoryNames, CancellationToken ct = default)
        {
            Calls++;
            Description = normalizedDescription;
            return Task.FromResult(suggestion);
        }
    }
}
