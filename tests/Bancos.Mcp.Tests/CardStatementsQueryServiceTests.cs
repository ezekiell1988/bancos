using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.CardStatements;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class CardStatementsQueryServiceTests
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
    public async Task Filters_statements_by_account_and_period_and_returns_linked_lines()
    {
        await using var db = await CreateDbAsync();
        var bank = new Bank { Id = Guid.NewGuid(), Code = "TEST", Name = "Banco de prueba" };
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankId = bank.Id,
            Code = "tarjeta-prueba",
            AccountType = "credit-card",
            CurrencyCode = "CRC"
        };
        var otherAccount = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankId = bank.Id,
            Code = "tarjeta-otra",
            AccountType = "credit-card",
            CurrencyCode = "CRC"
        };
        var statement = new CardStatement
        {
            Id = Guid.NewGuid(),
            BankAccountId = account.Id,
            StatementDate = new DateOnly(2026, 7, 18),
            PeriodLabel = "JUL-2026",
            CurrentBalanceCrc = 125000m,
            MinimumPaymentCrc = 5000m,
            SourceFingerprint = "statement-fingerprint"
        };
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = account.Id,
            TransactionDate = new DateOnly(2026, 7, 10),
            Description = "Compra de prueba",
            CurrencyCode = "CRC",
            Amount = 125000m,
            AmountCrc = 125000m,
            OperationType = "purchase",
            SourceFingerprint = "transaction-fingerprint"
        };
        db.Banks.Add(bank);
        db.BankAccounts.AddRange(account, otherAccount);
        db.CardStatements.Add(statement);
        db.Transactions.Add(transaction);
        db.CardStatementLines.Add(new CardStatementLine
        {
            Id = Guid.NewGuid(),
            CardStatementId = statement.Id,
            TransactionId = transaction.Id
        });
        db.CardStatements.Add(new CardStatement
        {
            Id = Guid.NewGuid(),
            BankAccountId = otherAccount.Id,
            StatementDate = new DateOnly(2026, 7, 18),
            PeriodLabel = "JUL-2026",
            SourceFingerprint = "other-statement-fingerprint"
        });
        await db.SaveChangesAsync();

        var result = await new CardStatementsQueryService(db).ListStatementsAsync(
            account.Id,
            "JUL-2026",
            null,
            null,
            page: 1,
            itemsPerPage: 50);

        var item = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(125000m, item.CurrentBalanceCrc);
        Assert.Equal("Banco de prueba", item.BankName);
        Assert.Equal(transaction.Id, Assert.Single(item.Lines).TransactionId);
        Assert.Equal("Compra de prueba", item.Lines[0].Description);
    }

    [Fact]
    public async Task Lists_only_active_financings_and_supports_currency_filter()
    {
        await using var db = await CreateDbAsync();
        var bank = new Bank { Id = Guid.NewGuid(), Code = "TEST", Name = "Banco de prueba" };
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankId = bank.Id,
            Code = "tarjeta-prueba",
            AccountType = "credit-card",
            CurrencyCode = "CRC"
        };
        db.Banks.Add(bank);
        db.BankAccounts.Add(account);
        db.CardFinancings.AddRange(
            new CardFinancing
            {
                Id = Guid.NewGuid(),
                BankAccountId = account.Id,
                FinancingDate = new DateOnly(2026, 7, 1),
                Concept = "Compra activa",
                CurrencyCode = "CRC",
                InitialBalance = 100000m,
                OutstandingBalance = 80000m,
                Installments = "2/10",
                InstallmentAmount = 10000m,
                Status = "active",
                SourceFingerprint = "active-fingerprint"
            },
            new CardFinancing
            {
                Id = Guid.NewGuid(),
                BankAccountId = account.Id,
                FinancingDate = new DateOnly(2026, 6, 1),
                Concept = "Plan liquidado",
                CurrencyCode = "CRC",
                InitialBalance = 50000m,
                OutstandingBalance = 0m,
                Installments = "10/10",
                InstallmentAmount = 5000m,
                Status = "settled",
                SourceFingerprint = "settled-fingerprint"
            });
        await db.SaveChangesAsync();

        var result = await new CardStatementsQueryService(db).ListActiveFinancingsAsync(
            account.Id,
            "CRC",
            page: 1,
            itemsPerPage: 50);

        var item = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Compra activa", item.Concept);
        Assert.Equal("active", item.Status);
    }
}