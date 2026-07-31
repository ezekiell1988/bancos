using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.AccountPeriodClosings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class CalculateAccountPeriodClosingsJobTests
{
    [Fact]
    public async Task Carries_forward_closing_for_account_without_new_movements()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var accounts = await db.BankAccounts.Take(2).ToListAsync();
        var firstPeriodId = Guid.NewGuid();
        var secondPeriodId = Guid.NewGuid();
        var thirdPeriodId = Guid.NewGuid();
        db.Periods.AddRange(
            new Period
            {
                Id = firstPeriodId,
                Label = "JUN-2026",
                StartDate = new DateOnly(2026, 6, 19),
                EndDate = new DateOnly(2026, 7, 18)
            },
            new Period
            {
                Id = secondPeriodId,
                Label = "JUL-2026",
                StartDate = new DateOnly(2026, 7, 19),
                EndDate = new DateOnly(2026, 8, 18)
            },
            new Period
            {
                Id = thirdPeriodId,
                Label = "AGO-2026",
                StartDate = new DateOnly(2026, 8, 19),
                EndDate = new DateOnly(2026, 9, 18)
            });
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = accounts[0].Id,
            TransactionDate = new DateOnly(2026, 8, 20),
            Description = "Movimiento de prueba",
            CurrencyCode = "CRC",
            Amount = -10m,
            AmountCrc = -10m,
            OperationType = "purchase",
            SourceFingerprint = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64]
        });
        db.AccountPeriodClosings.Add(new AccountPeriodClosing
        {
            Id = Guid.NewGuid(),
            BankAccountId = accounts[1].Id,
            PeriodId = firstPeriodId,
            Balance = 125m
        });
        await db.SaveChangesAsync();

        var job = new CalculateAccountPeriodClosingsJob(db, NullLogger<CalculateAccountPeriodClosingsJob>.Instance);
        await job.ExecuteAsync(secondPeriodId, null);

        var carriedForward = await db.AccountPeriodClosings.SingleAsync(closing =>
            closing.BankAccountId == accounts[1].Id && closing.PeriodId == secondPeriodId);
        Assert.Equal(125m, carriedForward.Balance);
        var carriedForwardAgain = await db.AccountPeriodClosings.SingleAsync(closing =>
            closing.BankAccountId == accounts[1].Id && closing.PeriodId == thirdPeriodId);
        Assert.Equal(125m, carriedForwardAgain.Balance);
    }

    [Fact]
    public async Task Creates_missing_periods_assigns_every_transaction_and_is_idempotent()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var accountId = (await db.BankAccounts.FirstAsync()).Id;
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = accountId,
            TransactionDate = new DateOnly(2024, 6, 20),
            Description = "Movimiento de prueba",
            CurrencyCode = "CRC",
            Amount = -10m,
            AmountCrc = -10m,
            OperationType = "purchase",
            SourceFingerprint = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64]
        });
        await db.SaveChangesAsync();

        var earliestSeededPeriodId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var job = new CalculateAccountPeriodClosingsJob(db, NullLogger<CalculateAccountPeriodClosingsJob>.Instance);
        await job.ExecuteAsync(earliestSeededPeriodId, null);
        var closingCount = await db.AccountPeriodClosings.CountAsync();
        await job.ExecuteAsync(earliestSeededPeriodId, null);

        var transactions = await db.Transactions.ToListAsync();
        var periods = await db.Periods.OrderBy(period => period.StartDate).ToListAsync();

        Assert.All(transactions, transaction => Assert.NotNull(transaction.PeriodId));
        Assert.Contains(periods, period => period.StartDate == new DateOnly(2024, 6, 19)
            && period.EndDate == new DateOnly(2024, 7, 18));
        Assert.All(periods.Zip(periods.Skip(1)), pair => Assert.Equal(pair.First.EndDate.AddDays(1), pair.Second.StartDate));
        Assert.Equal(closingCount, await db.AccountPeriodClosings.CountAsync());
    }
}
