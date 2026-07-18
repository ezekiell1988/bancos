namespace Bancos.Api.Domain;

public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
}

public sealed class Owner : AuditableEntity { public required string DisplayName { get; set; } public string? DocumentReference { get; set; } }
public sealed class Currency : AuditableEntity { public required string Code { get; set; } public required string Name { get; set; } }
public sealed class Account : AuditableEntity { public required string Code { get; set; } public required string Name { get; set; } public AccountKind Kind { get; set; } }
public sealed class AccountAuxiliary : AuditableEntity { public required string Name { get; set; } public string? Iban { get; set; } public Guid AccountId { get; set; } public Guid OwnerId { get; set; } public Account? Account { get; set; } public Owner? Owner { get; set; } }
public sealed class Import : AuditableEntity { public required string FileName { get; set; } public required string TemporaryPath { get; set; } public required string ContentHash { get; set; } public Guid AccountAuxiliaryId { get; set; } public AccountAuxiliary? AccountAuxiliary { get; set; } public string? Template { get; set; } public string? FailureReason { get; set; } public ImportStatus Status { get; set; } = ImportStatus.Queued; public DateTime? ProcessedUtc { get; set; } }
public sealed class ImportFingerprint : AuditableEntity { public required string Hash { get; set; } public Guid ImportId { get; set; } public Import? Import { get; set; } }
public sealed class ImportTemplatePattern : AuditableEntity { public required string SignatureHash { get; set; } public required string ContentKind { get; set; } public required string Template { get; set; } }
public sealed class Category : AuditableEntity { public required string Name { get; set; } public Guid? ParentId { get; set; } public Category? Parent { get; set; } }
public sealed class ClassificationRule : AuditableEntity { public Guid? AccountAuxiliaryId { get; set; } public required string Pattern { get; set; } public Guid CategoryId { get; set; } public bool IsApproved { get; set; } }
public sealed class ClassificationTag : AuditableEntity { public required string Name { get; set; } public Guid? TransactionId { get; set; } }
public sealed class Transaction : AuditableEntity { public Guid AccountAuxiliaryId { get; set; } public Guid ImportId { get; set; } public Import? Import { get; set; } public DateOnly BookingDate { get; set; } public required string ExternalReference { get; set; } public required string SourceFingerprint { get; set; } public decimal AmountCrc { get; set; } public decimal? OriginalAmount { get; set; } public string OriginalCurrencyCode { get; set; } = "CRC"; public decimal? ExchangeRate { get; set; } public string DescriptionNormalized { get; set; } = ""; public Guid? CategoryId { get; set; } public Category? Category { get; set; } public ClassificationSource ClassificationSource { get; set; } public ClassificationStatus ClassificationStatus { get; set; } = ClassificationStatus.PendingReview; }
public sealed class CreditFinancing : AuditableEntity { public Guid AccountAuxiliaryId { get; set; } public AccountAuxiliary? AccountAuxiliary { get; set; } public Guid ImportId { get; set; } public Import? Import { get; set; } public DateOnly FinancingDate { get; set; } public required string Concept { get; set; } public required string Installments { get; set; } public decimal InstallmentAmount { get; set; } public decimal InitialBalance { get; set; } public decimal OutstandingBalance { get; set; } public required string SourceFingerprint { get; set; } }
public sealed class LoanStatement : AuditableEntity { public Guid AccountAuxiliaryId { get; set; } public AccountAuxiliary? AccountAuxiliary { get; set; } public Guid ImportId { get; set; } public Import? Import { get; set; } public decimal OutstandingBalance { get; set; } public required string SourceFingerprint { get; set; } public ICollection<LoanPayment> Payments { get; set; } = []; }
public sealed class LoanPayment : AuditableEntity { public Guid LoanStatementId { get; set; } public LoanStatement? LoanStatement { get; set; } public DateOnly PaymentDate { get; set; } public decimal Capital { get; set; } public decimal Interest { get; set; } public decimal LateFee { get; set; } public decimal OtherCharges { get; set; } public decimal Total { get; set; } public required string SourceFingerprint { get; set; } }
public sealed class JournalEntry : AuditableEntity { public DateOnly PostingDate { get; set; } public required string Description { get; set; } public ICollection<JournalLine> Lines { get; set; } = []; }
public sealed class JournalLine : AuditableEntity { public Guid JournalEntryId { get; set; } public Guid AccountId { get; set; } public decimal DebitCrc { get; set; } public decimal CreditCrc { get; set; } }
public sealed class Reconciliation : AuditableEntity { public required string Reference { get; set; } public ICollection<ReconciliationTransaction> Transactions { get; set; } = []; }
public sealed class ReconciliationTransaction { public Guid ReconciliationId { get; set; } public Guid TransactionId { get; set; } public Reconciliation? Reconciliation { get; set; } public Transaction? Transaction { get; set; } }
public sealed class ExchangeRate : AuditableEntity { public DateOnly RateDate { get; set; } public required string CurrencyCode { get; set; } public decimal CrcPerUnit { get; set; } }
public sealed class ForeignExchangeClosing : AuditableEntity { public DateOnly PeriodEnd { get; set; } public ClosingStatus Status { get; set; } public ICollection<ForeignExchangeClosingLine> Lines { get; set; } = []; }
public sealed class ForeignExchangeClosingLine : AuditableEntity { public Guid ForeignExchangeClosingId { get; set; } public Guid AccountAuxiliaryId { get; set; } public decimal DifferenceCrc { get; set; } }
public sealed class ReportPeriod : AuditableEntity { public DateOnly PeriodEnd { get; set; } public bool IsStale { get; set; } = true; }
public sealed class AuditLog : AuditableEntity { public required string EntityName { get; set; } public required string EntityId { get; set; } public required string Action { get; set; } public string? Changes { get; set; } }

public enum AccountKind { Asset, Liability, Equity, Income, Expense, Control }
public enum ImportStatus { Queued, Processing, Completed, Failed }
public enum ClosingStatus { Pending, Processing, Completed, Failed }
public enum ClassificationSource { General, ExactApproved, Rule, Manual }
public enum ClassificationStatus { PendingReview, Approved }
