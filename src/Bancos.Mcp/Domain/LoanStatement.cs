namespace Bancos.Mcp.Domain;

public sealed class LoanStatement
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public DateOnly StatementDate { get; set; }
    public required string CurrencyCode { get; set; }
    public string? LoanNumber { get; set; }
    public decimal? OriginalLoanAmount { get; set; }
    public decimal? InterestRate { get; set; }
    public int? TermMonths { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? MaturityDate { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal? NextMonthCapital { get; set; }
    public decimal? NextMonthInterest { get; set; }
    public decimal? NextMonthTotal { get; set; }
    public decimal? CurrentPortionCapital { get; set; }
    public decimal? CurrentPortionInterest { get; set; }
    public decimal? CurrentPortionTotal { get; set; }
    public decimal? LongTermCapital { get; set; }
    public decimal? LongTermInterest { get; set; }
    public decimal? LongTermTotal { get; set; }
    public required string SourceFingerprint { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public BankAccount? BankAccount { get; set; }
    public ICollection<LoanPayment> Payments { get; set; } = [];
}

public sealed class LoanPayment
{
    public Guid Id { get; set; }
    public Guid LoanStatementId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Capital { get; set; }
    public decimal Interest { get; set; }
    public decimal LateFee { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal Total { get; set; }
    public decimal Balance { get; set; }
    public required string SourceFingerprint { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public LoanStatement? LoanStatement { get; set; }
}
