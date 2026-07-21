namespace Bancos.Mcp.Domain;

public sealed class ImportTemplate
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string ContentKind { get; set; }
    public required string ParserKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<ImportTemplatePattern> Patterns { get; set; } = [];
}

public sealed class ImportTemplatePattern
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string? SignatureHash { get; set; }
    public required string PatternKind { get; set; }
    public string? RequiredTermsJson { get; set; }
    public string? AlternativeTermGroupsJson { get; set; }
    public short DetectorVersion { get; set; } = 1;
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = CostaRicaTime.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ImportTemplate? Template { get; set; }
}

public static class CostaRicaTime
{
    private static readonly TimeZoneInfo Zone = FindZone();

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    private static TimeZoneInfo FindZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time"); }
    }
}