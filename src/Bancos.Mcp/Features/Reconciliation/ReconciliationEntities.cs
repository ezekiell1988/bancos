using Bancos.Mcp.Domain;

namespace Bancos.Mcp.Features.Reconciliation;

public static class ReconciliationStatuses
{
    public const string Proposed = "proposed";
    public const string Confirmed = "confirmed";
    public const string Deleted = "deleted";
}

public static class ReconciliationSides
{
    public const string Payment = "payment";
    public const string Transfer = "transfer";
}

public sealed class Reconciliation
{
    public Guid Id { get; set; }
    public string Status { get; set; } = ReconciliationStatuses.Proposed;
    public decimal Confidence { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public ICollection<ReconciliationItem> Items { get; set; } = [];
    public ICollection<ReconciliationAudit> AuditEntries { get; set; } = [];
}

public sealed class ReconciliationItem
{
    public Guid Id { get; set; }
    public Guid ReconciliationId { get; set; }
    public Guid TransactionId { get; set; }
    public string Side { get; set; } = string.Empty;
    public decimal AmountCrc { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public Reconciliation? Reconciliation { get; set; }
    public Transaction? Transaction { get; set; }
}

public sealed class ReconciliationAudit
{
    public Guid Id { get; set; }
    public Guid ReconciliationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? SnapshotJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public Reconciliation? Reconciliation { get; set; }
}