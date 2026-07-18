using Bancos.Api.Data;
using Bancos.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bancos.Api.Tests;
public sealed class BancosDbContextTests
{
    [Fact]
    public async Task SaveChanges_writes_an_audit_record_for_a_mutation()
    {
        await using var db = CreateContext(); db.Owners.Add(new Owner { DisplayName = "Test owner" }); await db.SaveChangesAsync();
        Assert.Single(await db.AuditLogs.Where(x => x.EntityName == nameof(Owner)).ToListAsync());
    }
    [Fact]
    public async Task Reconciliation_accepts_multiple_transactions()
    {
        await using var db = CreateContext(); var reconciliation = new Reconciliation { Reference = "test" }; reconciliation.Transactions.Add(new ReconciliationTransaction { TransactionId = Guid.NewGuid() }); reconciliation.Transactions.Add(new ReconciliationTransaction { TransactionId = Guid.NewGuid() }); db.Reconciliations.Add(reconciliation); await db.SaveChangesAsync();
        Assert.Equal(2, await db.ReconciliationTransactions.CountAsync());
    }
    private static BancosDbContext CreateContext() => new(new DbContextOptionsBuilder<BancosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
