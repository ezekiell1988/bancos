using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.Loans;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class LoansQueryServiceTests
{
    [Fact]
    public async Task Returns_only_payments_belonging_to_each_statement_in_date_order()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var bank = new Bank { Id = Guid.NewGuid(), Code = "TEST", Name = "Banco de prueba" };
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankId = bank.Id,
            Code = "prestamo-prueba",
            AccountType = "loan",
            CurrencyCode = "USD"
        };
        var statement = new LoanStatement
        {
            Id = Guid.NewGuid(),
            BankAccountId = account.Id,
            StatementDate = new DateOnly(2026, 7, 20),
            CurrencyCode = "USD",
            LoanNumber = "LOAN-001",
            OutstandingBalance = 9000m,
            SourceFingerprint = "loan-statement-fingerprint"
        };
        var otherStatement = new LoanStatement
        {
            Id = Guid.NewGuid(),
            BankAccountId = account.Id,
            StatementDate = new DateOnly(2026, 6, 20),
            CurrencyCode = "USD",
            LoanNumber = "LOAN-002",
            OutstandingBalance = 5000m,
            SourceFingerprint = "other-statement-fingerprint"
        };
        db.Banks.Add(bank);
        db.BankAccounts.Add(account);
        db.LoanStatements.AddRange(statement, otherStatement);
        db.LoanPayments.AddRange(
            new LoanPayment
            {
                Id = Guid.NewGuid(),
                LoanStatementId = statement.Id,
                InstallmentNumber = 2,
                PaymentDate = new DateOnly(2026, 8, 15),
                Capital = 100m,
                Interest = 20m,
                LateFee = 0m,
                OtherCharges = 0m,
                Total = 120m,
                Balance = 8900m,
                Status = "pending",
                SourceFingerprint = "payment-later-fingerprint"
            },
            new LoanPayment
            {
                Id = Guid.NewGuid(),
                LoanStatementId = statement.Id,
                InstallmentNumber = 1,
                PaymentDate = new DateOnly(2026, 7, 15),
                Capital = 100m,
                Interest = 20m,
                LateFee = 0m,
                OtherCharges = 0m,
                Total = 120m,
                Balance = 9000m,
                Status = "paid",
                SourceFingerprint = "payment-first-fingerprint"
            },
            new LoanPayment
            {
                Id = Guid.NewGuid(),
                LoanStatementId = otherStatement.Id,
                InstallmentNumber = 1,
                PaymentDate = new DateOnly(2026, 7, 1),
                Capital = 50m,
                Interest = 10m,
                LateFee = 0m,
                OtherCharges = 0m,
                Total = 60m,
                Balance = 4950m,
                Status = "pending",
                SourceFingerprint = "other-payment-fingerprint"
            });
        await db.SaveChangesAsync();

        var result = await new LoansQueryService(db).ListStatementsAsync(
            account.Id,
            "LOAN-001",
            null,
            null,
            page: 1,
            itemsPerPage: 50);

        var item = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(9000m, item.OutstandingBalance);
        Assert.Equal(2, item.Payments.Count);
        Assert.Equal(1, item.Payments[0].InstallmentNumber);
        Assert.Equal(2, item.Payments[1].InstallmentNumber);
        Assert.DoesNotContain("Fingerprint", string.Join(' ', item.GetType().GetProperties().Select(property => property.Name)));
    }
}