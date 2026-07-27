using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.FileProcessing;
using Bancos.Mcp.Features.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class BnCardStatementImportTests
{
    [Fact]
    public async Task Associates_transactions_with_statement_idempotently_and_routes_usd()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var bankId = (await db.Banks.FirstAsync(bank => bank.Code == "BN")).Id;
        var crcAccount = CreateCardAccount(bankId, "CRC");
        var usdAccount = CreateCardAccount(bankId, "USD");
        db.AddRange(crcAccount, usdAccount);
        await db.SaveChangesAsync();

        var parsed = new ParsedBnCardStatement(
            "****0000",
            "VISA",
            "PUNTOS",
            new DateOnly(2026, 7, 18),
            new DateOnly(2026, 8, 3),
            80m,
            5m,
            105m,
            10m,
            10m,
            1m,
            100m,
            10m,
            [
                new ParsedCardMovement(new DateOnly(2026, 7, 2), "crc-1", "CRC TEST", 25m, "CRC", 25m, CardOperationKind.Purchase),
                new ParsedCardMovement(new DateOnly(2026, 7, 3), "usd-1", "USD TEST", 5m, "USD", 2_500m, CardOperationKind.Purchase)
            ],
            []);
        var job = CreateJob(db);

        await job.ProcessBnCardStatement(crcAccount.Id, usdAccount.Id, parsed, null);
        await db.SaveChangesAsync();
        await job.ProcessBnCardStatement(crcAccount.Id, usdAccount.Id, parsed, null);
        await db.SaveChangesAsync();

        var statement = await db.CardStatements.Include(item => item.Lines).SingleAsync();
        var accountIds = new[] { crcAccount.Id, usdAccount.Id };
        var transactions = await db.Transactions
            .Where(item => accountIds.Contains(item.BankAccountId))
            .OrderBy(item => item.ReferenceNumber)
            .ToArrayAsync();
        Assert.Equal(2, statement.Lines.Count);
        Assert.Equal(parsed.PreviousBalanceCrc, statement.PreviousBalanceCrc);
        Assert.Equal(parsed.PreviousBalanceUsd, statement.PreviousBalanceUsd);
        Assert.Equal(parsed.CurrentBalanceCrc, statement.CurrentBalanceCrc);
        Assert.Equal(parsed.CurrentBalanceUsd, statement.CurrentBalanceUsd);
        Assert.Equal(2, transactions.Length);
        Assert.Equal(crcAccount.Id, transactions.Single(item => item.CurrencyCode == "CRC").BankAccountId);
        Assert.Equal(usdAccount.Id, transactions.Single(item => item.CurrencyCode == "USD").BankAccountId);
    }

    private static ImportFileJob CreateJob(McpCatalogDbContext db) => new(
        db,
        new BcrDebitCsvParser(),
        new AccountMovementSpreadsheetParser(),
        new BacCreditFinancingXlsParser(),
        new CardStatementParser(),
        new CoopealianzaLoanPdfParser(),
        new BacAccountStatementPdfParser(),
        new BnCardStatementPdfParser(),
        NullLogger<ImportFileJob>.Instance);

    private static BankAccount CreateCardAccount(Guid bankId, string currencyCode) => new()
    {
        Id = Guid.NewGuid(),
        BankId = bankId,
        Code = $"test-import-{currencyCode.ToLowerInvariant()}-{Guid.NewGuid():N}",
        AccountType = "credit-card",
        CurrencyCode = currencyCode
    };
}
