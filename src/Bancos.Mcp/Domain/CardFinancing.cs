namespace Bancos.Mcp.Domain;

public sealed class CardFinancing
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateOnly FinancingDate { get; set; }
    public required string Concept { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal OutstandingBalance { get; set; }
    public required string Installments { get; set; }
    public decimal InstallmentAmount { get; set; }
    public short? TermMonths { get; set; }
    public decimal? AnnualInterestRate { get; set; }
    public DateOnly? DueDate { get; set; }
    public required string Status { get; set; }
    public required string SourceFingerprint { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public BankAccount? BankAccount { get; set; }
}
