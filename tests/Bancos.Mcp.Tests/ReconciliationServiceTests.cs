using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class ReconciliationServiceTests
{
    [Fact]
    public async Task Proposal_confirm_correction_and_deletion_are_auditable_without_removing_transactions()
    {
        await using var db = CreateDb();
        var transactions = await SeedTransactionsAsync(db);
        var service = new ReconciliationService(db);

        var proposal = await service.ProposeAsync(
            [transactions[0].Id, transactions[1].Id],
            [transactions[2].Id, transactions[3].Id]);

        Assert.Equal(ReconciliationStatuses.Proposed, proposal.Status);
        Assert.Equal(4, proposal.Items.Count);
        Assert.Contains("CRC", proposal.Explanation);
        Assert.Contains("fechas", proposal.Explanation);
        Assert.Contains("confianza", proposal.Explanation);
        db.ChangeTracker.Clear();

        var confirmed = await service.ConfirmAsync(proposal.ReconciliationId, "usuario", "Coincide con el comprobante.");
        Assert.Equal(ReconciliationStatuses.Confirmed, confirmed.Status);
        Assert.Empty(await service.ListUnreconciledAsync(null, null, 200));

        var corrected = await service.CorrectAsync(
            proposal.ReconciliationId,
            [transactions[0].Id],
            [transactions[2].Id],
            "usuario",
            "Se retiro una partida que no correspondia.");
        Assert.Equal(ReconciliationStatuses.Confirmed, corrected.Status);
        Assert.Equal(2, corrected.Items.Count);

        var deleted = await service.DeleteAsync(proposal.ReconciliationId, "usuario", "Correccion reemplazada por otro comprobante.");
        Assert.Equal(ReconciliationStatuses.Deleted, deleted.Status);
        Assert.Equal(4, await db.Transactions.CountAsync());
        Assert.Equal(4, await db.Set<ReconciliationAudit>().CountAsync());
        Assert.Equal(
            new[] { "proposed", "confirmed", "corrected", "deleted" },
            await db.Set<ReconciliationAudit>().OrderBy(audit => audit.CreatedAt).Select(audit => audit.Action).ToArrayAsync());
        Assert.Equal(4, (await service.ListUnreconciledAsync(null, null, 200)).Count);
    }

    [Fact]
    public async Task Proposal_rejects_a_transaction_already_in_a_confirmed_reconciliation()
    {
        await using var db = CreateDb();
        var transactions = await SeedTransactionsAsync(db);
        var service = new ReconciliationService(db);

        var proposal = await service.ProposeAsync([transactions[0].Id], [transactions[2].Id]);
        await service.ConfirmAsync(proposal.ReconciliationId, "usuario", "Confirmado.");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProposeAsync([transactions[0].Id], [transactions[3].Id]));

        Assert.Contains("confirmada", exception.Message);
    }

    private static McpCatalogDbContext CreateDb() => new(new DbContextOptionsBuilder<McpCatalogDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static async Task<Transaction[]> SeedTransactionsAsync(McpCatalogDbContext db)
    {
        var bank = new Bank { Id = Guid.NewGuid(), Code = "BN", Name = "Banco" };
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankId = bank.Id,
            Code = "bn-debit-01-crc",
            AccountType = "debit-card",
            CurrencyCode = "CRC",
            Bank = bank
        };
        var transactions = new[]
        {
            NewTransaction(account, new DateOnly(2026, 7, 20), 100m, "Pago 1"),
            NewTransaction(account, new DateOnly(2026, 7, 20), 50m, "Pago 2"),
            NewTransaction(account, new DateOnly(2026, 7, 20), -80m, "Transferencia 1"),
            NewTransaction(account, new DateOnly(2026, 7, 21), -70m, "Transferencia 2")
        };
        db.AddRange(bank, account);
        db.AddRange(transactions);
        await db.SaveChangesAsync();
        return transactions;
    }

    private static Transaction NewTransaction(BankAccount account, DateOnly date, decimal amountCrc, string description) => new()
    {
        Id = Guid.NewGuid(),
        BankAccountId = account.Id,
        BankAccount = account,
        TransactionDate = date,
        Description = description,
        CurrencyCode = "CRC",
        Amount = amountCrc,
        AmountCrc = amountCrc,
        OperationType = amountCrc > 0 ? "payment" : "purchase",
        SourceFingerprint = Guid.NewGuid().ToString("N")
    };
}