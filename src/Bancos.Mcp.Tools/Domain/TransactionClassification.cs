namespace Bancos.Mcp.Domain;

public sealed class TransactionClassification
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ClassificationRuleId { get; set; }
    public required string Source { get; set; }
    public decimal? Confidence { get; set; }
    public string? Explanation { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public Transaction? Transaction { get; set; }
    public Category? Category { get; set; }
    public ClassificationRule? ClassificationRule { get; set; }
}
