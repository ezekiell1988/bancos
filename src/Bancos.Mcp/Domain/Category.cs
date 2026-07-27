namespace Bancos.Mcp.Domain;

public sealed class Category
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public required string RootType { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<ClassificationRule> Rules { get; set; } = [];
    public ICollection<TransactionClassification> Classifications { get; set; } = [];
}
