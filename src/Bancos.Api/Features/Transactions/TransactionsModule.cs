using Bancos.Api.Data;
using Bancos.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Api.Features.Transactions;

public static class TransactionsModule
{
    public static IEndpointRouteBuilder MapTransactionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions").WithTags("Transactions");
        group.MapGet("", ListTransactions);
        group.MapPatch("{id:guid}/category", PatchCategory);
        return app;
    }

    private static async Task<Ok<PagedTransactionResult>> ListTransactions(
        BancosDbContext db,
        Guid? categoryId,
        string? description,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = db.Transactions.AsNoTracking().AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(description))
        {
            var normalized = description.Trim().ToUpperInvariant();
            query = query.Where(x => x.DescriptionNormalized.Contains(normalized));
        }

        var totalAmount = await query.SumAsync(x => x.AmountCrc, ct);
        var count = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.BookingDate).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new TransactionRow(
                x.Id, x.BookingDate, x.DescriptionNormalized, x.AmountCrc,
                x.CategoryId, x.Category != null ? x.Category.Name : null))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedTransactionResult(items, count, totalAmount));
    }

    private static async Task<IResult> PatchCategory(Guid id, PatchCategoryRequest request, BancosDbContext db, CancellationToken ct)
    {
        var transaction = await db.Transactions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (transaction is null) return Results.NotFound();
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId, ct))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["categoryId"] = ["Category not found."] });

        transaction.CategoryId = request.CategoryId;
        transaction.ClassificationSource = ClassificationSource.Manual;
        transaction.ClassificationStatus = ClassificationStatus.Approved;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}

public sealed record TransactionRow(Guid Id, DateOnly BookingDate, string Description, decimal AmountCrc, Guid? CategoryId, string? CategoryName);
public sealed record PatchCategoryRequest(Guid CategoryId);
public sealed record PagedTransactionResult(List<TransactionRow> Items, int Count, decimal TotalAmount);
