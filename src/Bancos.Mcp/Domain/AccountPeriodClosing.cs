namespace Bancos.Mcp.Domain;

public sealed class AccountPeriodClosing
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public Guid PeriodId { get; set; }
    public decimal Balance { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public BankAccount? BankAccount { get; set; }
    public Period? Period { get; set; }
}
