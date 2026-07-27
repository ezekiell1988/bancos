namespace Bancos.Mcp.Domain;

public sealed class ClassificationRule
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BankAccountId { get; set; }
    public required string DescriptionPattern { get; set; }
    public required string MatchType { get; set; }
    public string? OperationType { get; set; }
    public string? Place { get; set; }
    public int Priority { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Category? Category { get; set; }
    public BankAccount? BankAccount { get; set; }
    public ICollection<TransactionClassification> Classifications { get; set; } = [];
}
