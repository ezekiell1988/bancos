using Bancos.Mcp.Data;
using Bancos.Mcp.Features.Accounts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class AccountsQueryServiceTests
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

    [Fact]
    public async Task Lists_bank_accounts_paginated_with_stable_order()
    {
        await using var db = await CreateDbAsync();
        var service = new AccountsQueryService(db);

        var totalAccounts = await db.BankAccounts.CountAsync();
        var firstPage = await service.ListBankAccountsAsync(onlyEnabled: true, page: 1, itemsPerPage: 5);
        var secondPage = await service.ListBankAccountsAsync(onlyEnabled: true, page: 2, itemsPerPage: 5);

        Assert.Equal(totalAccounts, firstPage.TotalItems);
        Assert.Equal(5, firstPage.Items.Count);
        Assert.Empty(firstPage.Items.Select(a => a.Id).Intersect(secondPage.Items.Select(a => a.Id)));
        Assert.All(firstPage.Items, a => Assert.False(string.IsNullOrWhiteSpace(a.BankName)));
    }

    [Fact]
    public async Task Excludes_disabled_accounts_when_only_enabled_is_true()
    {
        await using var db = await CreateDbAsync();
        var account = await db.BankAccounts.FirstAsync();
        account.IsEnabled = false;
        await db.SaveChangesAsync();

        var service = new AccountsQueryService(db);
        var enabledOnly = await service.ListBankAccountsAsync(onlyEnabled: true, page: 1, itemsPerPage: 200);
        var all = await service.ListBankAccountsAsync(onlyEnabled: false, page: 1, itemsPerPage: 200);

        Assert.DoesNotContain(enabledOnly.Items, a => a.Id == account.Id);
        Assert.Contains(all.Items, a => a.Id == account.Id);
    }

    [Fact]
    public async Task Lists_periods_paginated()
    {
        await using var db = await CreateDbAsync();
        var service = new AccountsQueryService(db);

        var totalPeriods = await db.Periods.CountAsync();
        var result = await service.ListPeriodsAsync(page: 1, itemsPerPage: 200);

        Assert.Equal(totalPeriods, result.TotalItems);
        Assert.Equal(totalPeriods, result.Items.Count);
        Assert.True(result.Items.SequenceEqual(result.Items.OrderBy(p => p.StartDate)));
    }
}
