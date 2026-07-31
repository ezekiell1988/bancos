using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.ExchangeRates;
using Bancos.Mcp.Features.ForeignExchange;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class ForeignExchangeServiceTests
{
    [Fact]
    public async Task Calculates_monthly_difference_only_for_usd_liability_accounts()
    {
        await using var db = new McpCatalogDbContext(new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var bank = new Bank { Id = Guid.NewGuid(), Code = "BN", Name = "Banco" };
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankId = bank.Id,
            Code = "prestamo-usd",
            AccountType = "loan",
            CurrencyCode = "USD"
        };
        var period = new Period
        {
            Id = Guid.NewGuid(),
            Label = "JUL-2026",
            StartDate = new DateOnly(2026, 6, 19),
            EndDate = new DateOnly(2026, 7, 18)
        };
        db.Banks.Add(bank);
        db.BankAccounts.Add(account);
        db.Periods.Add(period);
        db.ExchangeRates.AddRange(
            new ExchangeRate { Id = Guid.NewGuid(), BankId = bank.Id, RateDate = new DateOnly(2026, 6, 18), CurrencyCode = "USD", CrcPerUnit = 460m },
            new ExchangeRate { Id = Guid.NewGuid(), BankId = bank.Id, RateDate = new DateOnly(2026, 7, 18), CurrencyCode = "USD", CrcPerUnit = 470m });
        db.Transactions.AddRange(
            new Transaction
            {
                Id = Guid.NewGuid(),
                BankAccountId = account.Id,
                TransactionDate = new DateOnly(2026, 6, 18),
                Description = "Saldo inicial",
                CurrencyCode = "USD",
                Amount = -100m,
                AmountCrc = -46000m,
                ExchangeRate = 460m,
                OperationType = "other-charge",
                SourceFingerprint = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64]
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                BankAccountId = account.Id,
                PeriodId = period.Id,
                TransactionDate = new DateOnly(2026, 7, 1),
                Description = "Interés",
                CurrencyCode = "USD",
                Amount = -5m,
                AmountCrc = -2300m,
                ExchangeRate = 460m,
                OperationType = "interest",
                SourceFingerprint = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64]
            });
        await db.SaveChangesAsync();

        var service = new ForeignExchangeService(db, new ExchangeRateService(db));
        var result = await service.CalculateAsync(period.Id);

        Assert.NotNull(result);
        var line = Assert.Single(result!.Lines, candidate => candidate.AccountCode == "prestamo-usd");
        Assert.Equal(-1000m, line.DifferenceCrc);
        Assert.Equal(-5m, line.PeriodMovementUsd);
        Assert.Single(line.DocumentIds);
        Assert.DoesNotContain(result.Warnings, warning => warning.Contains("No existe", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Reports_warning_when_a_required_rate_is_missing()
    {
        await using var db = new McpCatalogDbContext(new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var bank = new Bank { Id = Guid.NewGuid(), Code = "BN", Name = "Banco" };
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankId = bank.Id,
            Code = "prestamo-usd",
            AccountType = "loan",
            CurrencyCode = "USD"
        };
        var period = new Period
        {
            Id = Guid.NewGuid(),
            Label = "JUL-2026",
            StartDate = new DateOnly(2026, 6, 19),
            EndDate = new DateOnly(2026, 7, 18)
        };
        db.Banks.Add(bank);
        db.BankAccounts.Add(account);
        db.Periods.Add(period);
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = account.Id,
            TransactionDate = new DateOnly(2026, 6, 18),
            Description = "Saldo inicial",
            CurrencyCode = "USD",
            Amount = -100m,
            AmountCrc = -46000m,
            ExchangeRate = 460m,
            OperationType = "other-charge",
            SourceFingerprint = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64]
        });
        await db.SaveChangesAsync();

        var service = new ForeignExchangeService(db, new ExchangeRateService(db));
        var result = await service.CalculateAsync(period.Id);

        Assert.NotNull(result);
        Assert.Equal("completed_with_warnings", result!.Status);
        var line = Assert.Single(result.Lines);
        Assert.Null(line.DifferenceCrc);
        Assert.Contains(result.Warnings, warning => warning.Contains("No existe tipo de cambio", StringComparison.Ordinal));
    }
}