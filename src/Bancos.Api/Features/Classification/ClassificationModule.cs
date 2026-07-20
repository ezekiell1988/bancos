using Bancos.Api.Data;
using Bancos.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Api.Features.Classification;

public static class ClassificationModule
{
    public static IServiceCollection AddClassificationModule(this IServiceCollection services)
    {
        services.AddOptions<ClassificationAiOptions>().BindConfiguration(ClassificationAiOptions.Section);
        services.AddHttpClient<IFamilyCategorySuggester, AzureAiFamilyCategorySuggester>();
        services.AddScoped<ClassificationService>();
        return services;
    }

    public static IEndpointRouteBuilder MapClassificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/classification").WithTags("Classification");
        group.MapPost("/categories", CreateCategory);
        group.MapGet("/categories", ListCategories);
        group.MapGet("/rules", ListRules);
        group.MapPost("/rules", CreateRule);
        group.MapGet("/transactions/pending", ListPendingTransactions);
        group.MapPut("/transactions/{id:guid}/review", ReviewTransaction);
        return app;
    }

    private static async Task<IResult> CreateCategory(CreateCategoryRequest request, BancosDbContext db, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Name is required."] });
        if (request.ParentId is Guid parentId && !await db.Categories.AnyAsync(x => x.Id == parentId, ct)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentId"] = ["The parent category was not found."] });
        if (await db.Categories.AnyAsync(x => x.Name == name && x.ParentId == request.ParentId, ct)) return Results.Conflict();
        var category = new Category { Name = name, ParentId = request.ParentId }; db.Categories.Add(category); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/classification/categories/{category.Id}", new CategoryResponse(category.Id, category.Name, category.ParentId));
    }

    private static async Task<IResult> ListRules(BancosDbContext db, CancellationToken ct) => Results.Ok(await db.ClassificationRules.AsNoTracking()
        .OrderByDescending(x => x.IsApproved).ThenByDescending(x => x.AccountAuxiliaryId.HasValue).ThenByDescending(x => x.Pattern.Length).ThenBy(x => x.CreatedUtc).ThenBy(x => x.Id)
        .Select(x => new ClassificationRuleResponse(x.Id, x.AccountAuxiliaryId, x.Pattern, x.CategoryId, x.IsApproved)).ToListAsync(ct));

    private static async Task<Ok<List<CategoryResponse>>> ListCategories(BancosDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Categories.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new CategoryResponse(x.Id, x.Name, x.ParentId)).ToListAsync(ct));

    private static async Task<Ok<List<PendingTransactionResponse>>> ListPendingTransactions(BancosDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Transactions.AsNoTracking()
            .Where(x => x.ClassificationStatus == ClassificationStatus.PendingReview)
            .OrderBy(x => x.BookingDate).ThenBy(x => x.Id)
            .Select(x => new PendingTransactionResponse(x.Id, x.BookingDate, x.DescriptionNormalized, x.AmountCrc, x.OriginalCurrencyCode, x.AccountAuxiliaryId, x.ImportId))
            .ToListAsync(ct));

    private static async Task<IResult> CreateRule(CreateClassificationRuleRequest request, BancosDbContext db, CancellationToken ct)
    {
        var pattern = request.Pattern.Trim();
        if (string.IsNullOrWhiteSpace(pattern)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["pattern"] = ["Pattern is required."] });
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId, ct)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["categoryId"] = ["The category was not found."] });
        if (request.AccountAuxiliaryId is Guid accountId && !await db.AccountAuxiliaries.AnyAsync(x => x.Id == accountId, ct)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["accountAuxiliaryId"] = ["The account auxiliary was not found."] });
        var rule = new ClassificationRule { AccountAuxiliaryId = request.AccountAuxiliaryId, Pattern = ImportPattern.Normalize(pattern), CategoryId = request.CategoryId, IsApproved = request.IsApproved };
        db.ClassificationRules.Add(rule); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/classification/rules/{rule.Id}", new ClassificationRuleResponse(rule.Id, rule.AccountAuxiliaryId, rule.Pattern, rule.CategoryId, rule.IsApproved));
    }

    private static async Task<IResult> ReviewTransaction(Guid id, ReviewClassificationRequest request, BancosDbContext db, CancellationToken ct)
    {
        var transaction = await db.Transactions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (transaction is null) return Results.NotFound();
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId, ct)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["categoryId"] = ["The category was not found."] });
        transaction.CategoryId = request.CategoryId; transaction.ClassificationSource = ClassificationSource.Manual; transaction.ClassificationStatus = ClassificationStatus.Approved;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new ClassificationResponse(transaction.Id, transaction.CategoryId, transaction.ClassificationSource, transaction.ClassificationStatus));
    }
}

public sealed class ClassificationService(BancosDbContext db, IFamilyCategorySuggester? ai = null)
{
    public async Task ClassifyAsync(Transaction transaction, CancellationToken ct = default)
    {
        var localExact = db.Transactions.Local.Where(x => x.AccountAuxiliaryId == transaction.AccountAuxiliaryId && x.DescriptionNormalized == transaction.DescriptionNormalized && x.ClassificationStatus == ClassificationStatus.Approved && x.CategoryId != null)
            .OrderByDescending(x => x.UpdatedUtc ?? x.CreatedUtc).ThenByDescending(x => x.Id).Select(x => x.CategoryId).FirstOrDefault();
        if (localExact is Guid localCategory) { Assign(transaction, localCategory, ClassificationSource.ExactApproved, ClassificationStatus.Approved); return; }

        var exact = await db.Transactions.AsNoTracking().Where(x => x.AccountAuxiliaryId == transaction.AccountAuxiliaryId && x.DescriptionNormalized == transaction.DescriptionNormalized && x.ClassificationStatus == ClassificationStatus.Approved && x.CategoryId != null)
            .OrderByDescending(x => x.UpdatedUtc ?? x.CreatedUtc).ThenByDescending(x => x.Id).Select(x => x.CategoryId).FirstOrDefaultAsync(ct);
        if (exact is Guid exactCategory) { Assign(transaction, exactCategory, ClassificationSource.ExactApproved, ClassificationStatus.Approved); return; }

        var rules = await db.ClassificationRules.AsNoTracking().Where(x => x.IsApproved && (x.AccountAuxiliaryId == null || x.AccountAuxiliaryId == transaction.AccountAuxiliaryId))
            .OrderByDescending(x => x.AccountAuxiliaryId == transaction.AccountAuxiliaryId).ThenByDescending(x => x.Pattern.Length).ThenBy(x => x.CreatedUtc).ThenBy(x => x.Id).ToListAsync(ct);
        var matched = rules.FirstOrDefault(x => ImportPattern.IsMatch(transaction.DescriptionNormalized, x.Pattern));
        if (matched is not null) { Assign(transaction, matched.CategoryId, ClassificationSource.Rule, ClassificationStatus.Approved); return; }

        var storedCategories = await db.Categories.Where(x => x.ParentId == null && x.Name != "General").ToListAsync(ct);
        var categories = storedCategories.Concat(db.Categories.Local.Where(x => x.ParentId == null && x.Name != "General"))
            .DistinctBy(x => x.Id).OrderBy(x => x.Name).ToList();
        if (ai is not null)
        {
            var suggestion = await ai.SuggestAsync(transaction.DescriptionNormalized, categories.Select(x => x.Name).ToArray(), ct);
            var suggestedName = suggestion is null ? null : NormalizeCategoryName(suggestion.CategoryName);
            if (suggestedName is not null && !suggestedName.Equals("General", StringComparison.OrdinalIgnoreCase))
            {
                var category = categories.FirstOrDefault(x => x.Name.Equals(suggestedName, StringComparison.OrdinalIgnoreCase));
                if (category is not null || suggestion!.CreateNew)
                {
                    category ??= new Category { Name = suggestedName };
                    if (db.Entry(category).State == EntityState.Detached) db.Categories.Add(category);
                    Assign(transaction, category.Id, ClassificationSource.Ai, ClassificationStatus.Approved);
                    return;
                }
            }
        }

        var general = db.Categories.Local.FirstOrDefault(x => x.Name == "General" && x.ParentId == null)
            ?? await db.Categories.FirstOrDefaultAsync(x => x.Name == "General" && x.ParentId == null, ct);
        if (general is null) { general = new Category { Name = "General" }; db.Categories.Add(general); }
        Assign(transaction, general.Id, ClassificationSource.General, ClassificationStatus.PendingReview);
    }

    private static void Assign(Transaction transaction, Guid categoryId, ClassificationSource source, ClassificationStatus status)
    {
        transaction.CategoryId = categoryId; transaction.ClassificationSource = source; transaction.ClassificationStatus = status;
    }

    private static string? NormalizeCategoryName(string value)
    {
        var name = string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return name.Length is < 2 or > 80 || name.Any(char.IsControl) ? null : name;
    }
}

public static class ImportPattern
{
    public static string Normalize(string value) => string.Join(' ', value.Trim().ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    public static bool IsMatch(string description, string pattern) => description.Contains(pattern, StringComparison.Ordinal);
}

public sealed record CreateClassificationRuleRequest(Guid? AccountAuxiliaryId, string Pattern, Guid CategoryId, bool IsApproved = true);
public sealed record ClassificationRuleResponse(Guid Id, Guid? AccountAuxiliaryId, string Pattern, Guid CategoryId, bool IsApproved);
public sealed record CreateCategoryRequest(string Name, Guid? ParentId);
public sealed record CategoryResponse(Guid Id, string Name, Guid? ParentId);
public sealed record ReviewClassificationRequest(Guid CategoryId);
public sealed record ClassificationResponse(Guid TransactionId, Guid? CategoryId, ClassificationSource Source, ClassificationStatus Status);
public sealed record PendingTransactionResponse(Guid Id, DateOnly BookingDate, string Description, decimal AmountCrc, string CurrencyCode, Guid AccountAuxiliaryId, Guid ImportId);
