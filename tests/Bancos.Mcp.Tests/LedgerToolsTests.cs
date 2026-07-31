using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.Ledger;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class LedgerToolsTests
{
    [Fact]
    public async Task Returns_one_traceable_voucher_and_line_per_period_movement()
    {
        await using var db = new McpCatalogDbContext(new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        await db.Database.EnsureCreatedAsync();

        var bank = new Bank { Id = Guid.NewGuid(), Code = "BN", Name = "Banco" };
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankId = bank.Id,
            Code = "cuenta-prueba",
            AccountType = "debit-card",
            CurrencyCode = "CRC"
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
            PeriodId = period.Id,
            TransactionDate = new DateOnly(2026, 7, 1),
            ReferenceNumber = "REF-1",
            Description = "Movimiento de prueba",
            CurrencyCode = "CRC",
            Amount = -100m,
            AmountCrc = -100m,
            OperationType = "purchase",
            SourceFingerprint = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64]
        });
        await db.SaveChangesAsync();

        var result = await new LedgerQueryService(db).GetPeriodAsync(period.Id);

        Assert.NotNull(result);
        Assert.Single(result!.Vouchers);
        Assert.Single(result.Vouchers[0].Lines);
        Assert.Equal("cuenta-prueba", result.Vouchers[0].Lines[0].AccountCode);
        Assert.Empty(result.Warnings);
    }
}