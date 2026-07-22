namespace Bancos.Mcp.Domain;

public sealed class Transaction
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public Guid? PeriodId { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateOnly TransactionDate { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public required string Description { get; set; }
    public string? Place { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountCrc { get; set; }
    public decimal? ExchangeRate { get; set; }
    public required string OperationType { get; set; }
    public required string SourceFingerprint { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public BankAccount? BankAccount { get; set; }
    public Period? Period { get; set; }
    public ICollection<CardStatementLine> CardStatementLines { get; set; } = [];
}
