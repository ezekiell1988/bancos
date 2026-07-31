using Bancos.Mcp.Data;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.Accounts;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int ItemsPerPage, int TotalItems);

public sealed record BankAccountSummary(
    Guid Id,
    string BankName,
    string BankCode,
    string AccountCode,
    string AccountType,
    string CurrencyCode,
    bool IsEnabled);

public sealed record PeriodSummary(Guid Id, string Label, DateOnly StartDate, DateOnly EndDate);

public sealed class AccountsQueryService(McpCatalogDbContext db)
{
    public async Task<PagedResult<BankAccountSummary>> ListBankAccountsAsync(
        bool onlyEnabled, int page, int itemsPerPage, CancellationToken ct = default)
    {
        var query = db.BankAccounts.AsQueryable();
        if (onlyEnabled)
            query = query.Where(a => a.IsEnabled);

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .Include(a => a.Bank)
            .OrderBy(a => a.Bank!.Name).ThenBy(a => a.Code).ThenBy(a => a.Id)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .Select(a => new BankAccountSummary(
                a.Id,
                a.Bank!.Name,
                a.Bank.Code,
                a.Code,
                a.AccountType,
                a.CurrencyCode,
                a.IsEnabled))
            .ToListAsync(ct);

        return new PagedResult<BankAccountSummary>(items, page, itemsPerPage, totalItems);
    }

    public async Task<PagedResult<PeriodSummary>> ListPeriodsAsync(
        int page, int itemsPerPage, CancellationToken ct = default)
    {
        var query = db.Periods.AsQueryable();
        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.StartDate).ThenBy(p => p.Id)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .Select(p => new PeriodSummary(p.Id, p.Label, p.StartDate, p.EndDate))
            .ToListAsync(ct);

        return new PagedResult<PeriodSummary>(items, page, itemsPerPage, totalItems);
    }
}
