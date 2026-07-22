namespace Bancos.Mcp.Domain;

public sealed class Period
{
    public Guid Id { get; set; }
    public required string Label { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = [];
}
