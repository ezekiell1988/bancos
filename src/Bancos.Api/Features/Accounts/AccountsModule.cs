using Bancos.Api.Data;
using Bancos.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Api.Features.Accounts;

public static class AccountsModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts");
        group.MapPost("/owners", CreateOwner);
        group.MapPost("/", CreateAccount);
        group.MapPost("/auxiliaries", CreateAuxiliary);
        group.MapGet("/auxiliaries/{id:guid}", GetAuxiliary);
        return app;
    }

    private static async Task<Created<OwnerResponse>> CreateOwner(CreateOwnerRequest request, BancosDbContext db, CancellationToken ct)
    {
        var owner = new Owner { DisplayName = request.DisplayName.Trim(), DocumentReference = request.DocumentReference?.Trim() };
        db.Owners.Add(owner); await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/accounts/owners/{owner.Id}", new OwnerResponse(owner.Id, owner.DisplayName));
    }

    private static async Task<Created<AccountResponse>> CreateAccount(CreateAccountRequest request, BancosDbContext db, CancellationToken ct)
    {
        var account = new Account { Code = request.Code.Trim(), Name = request.Name.Trim(), Kind = request.Kind };
        db.Accounts.Add(account); await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/accounts/{account.Id}", new AccountResponse(account.Id, account.Code, account.Name, account.Kind));
    }

    private static async Task<Results<Created<AccountAuxiliaryResponse>, ValidationProblem>> CreateAuxiliary(CreateAccountAuxiliaryRequest request, BancosDbContext db, CancellationToken ct)
    {
        if (!await db.Owners.AnyAsync(x => x.Id == request.OwnerId, ct) || !await db.Accounts.AnyAsync(x => x.Id == request.AccountId, ct))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["ownerId/accountId"] = ["The owner or account was not found."] });
        var auxiliary = new AccountAuxiliary { Name = request.Name.Trim(), Iban = request.Iban?.Trim().ToUpperInvariant(), OwnerId = request.OwnerId, AccountId = request.AccountId };
        db.AccountAuxiliaries.Add(auxiliary); await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/accounts/auxiliaries/{auxiliary.Id}", ToResponse(auxiliary));
    }

    private static async Task<Results<Ok<AccountAuxiliaryResponse>, NotFound>> GetAuxiliary(Guid id, BancosDbContext db, CancellationToken ct)
    {
        var auxiliary = await db.AccountAuxiliaries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return auxiliary is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(auxiliary));
    }

    private static AccountAuxiliaryResponse ToResponse(AccountAuxiliary value) => new(value.Id, value.Name, value.Iban, value.OwnerId, value.AccountId);
}

public sealed record CreateOwnerRequest(string DisplayName, string? DocumentReference);
public sealed record OwnerResponse(Guid Id, string DisplayName);
public sealed record CreateAccountRequest(string Code, string Name, AccountKind Kind);
public sealed record AccountResponse(Guid Id, string Code, string Name, AccountKind Kind);
public sealed record CreateAccountAuxiliaryRequest(string Name, string? Iban, Guid OwnerId, Guid AccountId);
public sealed record AccountAuxiliaryResponse(Guid Id, string Name, string? Iban, Guid OwnerId, Guid AccountId);
