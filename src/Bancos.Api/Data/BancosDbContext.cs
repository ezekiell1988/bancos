using Bancos.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Api.Data;

public sealed class BancosDbContext(DbContextOptions<BancosDbContext> options) : DbContext(options)
{
    public DbSet<Owner> Owners => Set<Owner>(); public DbSet<Currency> Currencies => Set<Currency>(); public DbSet<Account> Accounts => Set<Account>(); public DbSet<AccountAuxiliary> AccountAuxiliaries => Set<AccountAuxiliary>(); public DbSet<Import> Imports => Set<Import>(); public DbSet<ImportFingerprint> ImportFingerprints => Set<ImportFingerprint>(); public DbSet<Category> Categories => Set<Category>(); public DbSet<ClassificationRule> ClassificationRules => Set<ClassificationRule>(); public DbSet<ClassificationTag> ClassificationTags => Set<ClassificationTag>(); public DbSet<Transaction> Transactions => Set<Transaction>(); public DbSet<CreditFinancing> CreditFinancings => Set<CreditFinancing>(); public DbSet<LoanStatement> LoanStatements => Set<LoanStatement>(); public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>(); public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>(); public DbSet<JournalLine> JournalLines => Set<JournalLine>(); public DbSet<Reconciliation> Reconciliations => Set<Reconciliation>(); public DbSet<ReconciliationTransaction> ReconciliationTransactions => Set<ReconciliationTransaction>(); public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>(); public DbSet<ForeignExchangeClosing> ForeignExchangeClosings => Set<ForeignExchangeClosing>(); public DbSet<ForeignExchangeClosingLine> ForeignExchangeClosingLines => Set<ForeignExchangeClosingLine>(); public DbSet<ReportPeriod> ReportPeriods => Set<ReportPeriod>(); public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Owner>().HasIndex(x => x.DocumentReference).IsUnique().HasFilter("[DocumentReference] IS NOT NULL");
        b.Entity<Currency>().HasIndex(x => x.Code).IsUnique(); b.Entity<Account>().HasIndex(x => x.Code).IsUnique();
        b.Entity<AccountAuxiliary>().HasIndex(x => x.Iban).IsUnique().HasFilter("[Iban] IS NOT NULL");
        b.Entity<ImportFingerprint>().HasIndex(x => x.Hash).IsUnique(); b.Entity<Import>().HasIndex(x => x.ContentHash);
        b.Entity<Transaction>().HasIndex(x => new { x.AccountAuxiliaryId, x.SourceFingerprint }).IsUnique();
        b.Entity<CreditFinancing>().HasIndex(x => new { x.AccountAuxiliaryId, x.SourceFingerprint }).IsUnique();
        b.Entity<LoanStatement>().HasIndex(x => new { x.AccountAuxiliaryId, x.SourceFingerprint }).IsUnique();
        b.Entity<LoanPayment>().HasIndex(x => new { x.LoanStatementId, x.SourceFingerprint }).IsUnique();
        b.Entity<Category>().HasIndex(x => new { x.Name, x.ParentId }).IsUnique(); b.Entity<ClassificationTag>().HasIndex(x => x.Name).IsUnique();
        b.Entity<ExchangeRate>().HasIndex(x => new { x.RateDate, x.CurrencyCode }).IsUnique(); b.Entity<ReportPeriod>().HasIndex(x => x.PeriodEnd).IsUnique();
        b.Entity<JournalLine>().ToTable(t => t.HasCheckConstraint("CK_JournalLines_OneSide", "([DebitCrc] = 0 AND [CreditCrc] > 0) OR ([CreditCrc] = 0 AND [DebitCrc] > 0)"));
        b.Entity<ReconciliationTransaction>().HasKey(x => new { x.ReconciliationId, x.TransactionId });
        b.Entity<ReconciliationTransaction>().HasOne(x => x.Reconciliation).WithMany(x => x.Transactions).HasForeignKey(x => x.ReconciliationId);
        b.Entity<ReconciliationTransaction>().HasOne(x => x.Transaction).WithMany().HasForeignKey(x => x.TransactionId);
        b.Entity<Transaction>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CreditFinancing>().HasOne(x => x.Import).WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<LoanStatement>().HasOne(x => x.Import).WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        SeedDefaults(b);
    }

    private static void SeedDefaults(ModelBuilder b)
    {
        var created = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var ownerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var assetId = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var liabilityId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        b.Entity<Owner>().HasData(new Owner { Id = ownerId, DisplayName = "Propietario predeterminado", CreatedUtc = created });
        b.Entity<Account>().HasData(
            new Account { Id = assetId, Code = "1", Name = "Activo", Kind = AccountKind.Asset, CreatedUtc = created },
            new Account { Id = liabilityId, Code = "2", Name = "Pasivo", Kind = AccountKind.Liability, CreatedUtc = created },
            new Account { Id = Guid.Parse("00000000-0000-0000-0000-000000000103"), Code = "3", Name = "Capital", Kind = AccountKind.Equity, CreatedUtc = created },
            new Account { Id = Guid.Parse("00000000-0000-0000-0000-000000000104"), Code = "4", Name = "Ingreso", Kind = AccountKind.Income, CreatedUtc = created },
            new Account { Id = Guid.Parse("00000000-0000-0000-0000-000000000105"), Code = "5", Name = "Gasto", Kind = AccountKind.Expense, CreatedUtc = created },
            new Account { Id = Guid.Parse("00000000-0000-0000-0000-000000000106"), Code = "6", Name = "Control", Kind = AccountKind.Control, CreatedUtc = created });
        b.Entity<AccountAuxiliary>().HasData(
            new AccountAuxiliary { Id = Guid.Parse("00000000-0000-0000-0000-000000000201"), Name = "Cuenta bancaria", AccountId = assetId, OwnerId = ownerId, CreatedUtc = created },
            new AccountAuxiliary { Id = Guid.Parse("00000000-0000-0000-0000-000000000202"), Name = "Créditos y financiamientos", AccountId = liabilityId, OwnerId = ownerId, CreatedUtc = created });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ChangeTracker.DetectChanges(); var now = DateTime.UtcNow;
        var entries = ChangeTracker.Entries<AuditableEntity>().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLog) continue;
            if (entry.State == EntityState.Modified) entry.Entity.UpdatedUtc = now;
            AuditLogs.Add(new AuditLog { EntityName = entry.Metadata.ClrType.Name, EntityId = entry.Entity.Id.ToString(), Action = entry.State.ToString(), Changes = entry.State == EntityState.Modified ? string.Join(',', entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name)) : null });
        }
        return await base.SaveChangesAsync(ct);
    }
}
