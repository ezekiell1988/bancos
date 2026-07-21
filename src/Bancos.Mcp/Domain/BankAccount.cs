namespace Bancos.Mcp.Domain;

public sealed class Bank
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<BankAccount> Accounts { get; set; } = [];
    public ICollection<ExchangeRate> ExchangeRates { get; set; } = [];
}

public sealed class BankAccount
{
    public Guid Id { get; set; }
    public Guid BankId { get; set; }
    public required string Code { get; set; }
    public string? IdentifierHash { get; set; }
    public string? CardFingerprint { get; set; }
    public required string AccountType { get; set; }
    public required string CurrencyCode { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Bank? Bank { get; set; }
    public ICollection<BankAccountImportTemplate> ImportTemplates { get; set; } = [];
}

public sealed class BankAccountImportTemplate
{
    public Guid BankAccountId { get; set; }
    public Guid ImportTemplateId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public ImportTemplate? ImportTemplate { get; set; }
}

public sealed class ExchangeRate
{
    public Guid Id { get; set; }
    public Guid BankId { get; set; }
    public DateOnly RateDate { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal CrcPerUnit { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public Bank? Bank { get; set; }
}