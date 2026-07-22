namespace Bancos.Mcp.Domain;

public sealed class CardStatement
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public DateOnly StatementDate { get; set; }
    public required string PeriodLabel { get; set; }
    public DateOnly? MinimumPaymentDueDate { get; set; }
    public DateOnly? CashPaymentDueDate { get; set; }
    public decimal PreviousBalanceCrc { get; set; }
    public decimal PreviousBalanceUsd { get; set; }
    public decimal PurchasesTotalCrc { get; set; }
    public decimal PurchasesTotalUsd { get; set; }
    public decimal PaymentsTotalCrc { get; set; }
    public decimal PaymentsTotalUsd { get; set; }
    public decimal InterestTotalCrc { get; set; }
    public decimal InterestTotalUsd { get; set; }
    public decimal CurrentBalanceCrc { get; set; }
    public decimal CurrentBalanceUsd { get; set; }
    public decimal MinimumPaymentCrc { get; set; }
    public decimal MinimumPaymentUsd { get; set; }
    public decimal CashPaymentCrc { get; set; }
    public decimal CashPaymentUsd { get; set; }
    public decimal CreditLimitCrc { get; set; }
    public decimal CreditLimitUsd { get; set; }
    public decimal AvailableBalanceCrc { get; set; }
    public decimal AvailableBalanceUsd { get; set; }
    public required string SourceFingerprint { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public BankAccount? BankAccount { get; set; }
    public ICollection<CardStatementLine> Lines { get; set; } = [];
}

public sealed class CardStatementLine
{
    public Guid Id { get; set; }
    public Guid CardStatementId { get; set; }
    public Guid TransactionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public CardStatement? CardStatement { get; set; }
    public Transaction? Transaction { get; set; }
}
