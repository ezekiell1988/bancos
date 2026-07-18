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

    private static BancosDbContext CreateContext() => new(new DbContextOptionsBuilder<BancosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Transaction NewTransaction(Guid accountId, string description) => new() { AccountAuxiliaryId = accountId, ImportId = Guid.NewGuid(), BookingDate = new DateOnly(2026, 1, 2), ExternalReference = Guid.NewGuid().ToString(), SourceFingerprint = Guid.NewGuid().ToString(), AmountCrc = 1, DescriptionNormalized = description };
}
