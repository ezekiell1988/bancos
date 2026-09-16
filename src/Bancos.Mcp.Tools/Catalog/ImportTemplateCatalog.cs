using Bancos.Mcp.Domain;

namespace Bancos.Mcp.Catalog;

public sealed record ImportTemplateDefinition(
    Guid Id,
    string Code,
    string Name,
    string ContentKind,
    string ParserKey,
    string[] RequiredTerms,
    string[][]? AlternativeTermGroups = null,
    string[]? ExcludedTerms = null);

/// <summary>Catálogo inmutable que es la única fuente de IDs, metadatos y firmas de plantillas.</summary>
public static class ImportTemplateCatalog
{
    public static readonly DateTimeOffset SeededAt = new(2026, 7, 20, 0, 0, 0, TimeSpan.FromHours(-6));

    public static IReadOnlyList<ImportTemplateDefinition> Definitions { get; } =
    [
        Definition(1, "bcr-debit-csv-v1", "Movimientos de cuenta BCR", "csv", "bcr-debit-csv", [";", "oficina", "fechamovimiento", "numerodocumento", "debito", "credito", "descripcion"]),
        Definition(2, "bac-credit-csv-v1", "Resumen de tarjeta BAC", "csv", "bac-credit-csv", [",", "name", "date", "minimum payment", "cash payment", "local amount"], [["dollar amount", "dollars amount"]]),
        Definition(3, "bcr-debit-html-xls-v1", "Movimientos BCR HTML", "html", "bcr-debit-html", ["banco de costa rica"], [["movimientos por rango de fechas", "movimientos de la cuenta", "movimientos del d"]]),
        Definition(4, "bank-account-movements-xls-v1", "Movimientos de cuenta XLS", "xls", "bank-account-movements-xls", ["fecha"], [["descripcion", "detalle"], ["debito", "debitos"], ["credito", "creditos"]]),
        Definition(5, "bac-credit-financing-xls-v1", "Financiamientos BAC", "xls", "bac-credit-financing-xls", ["consulta de financiamientos", "fecha", "concepto", "cuotas", "monto de cuota", "saldo inicial", "saldo faltante"]),
        Definition(6, "bac-credit-online-pdf-v1", "Tarjeta BAC en linea", "pdf", "bac-credit-online-pdf", ["tarjeta de credito", "saldo en colones", "saldo en dolares", "fecha de pago de contado"], null, ["total pago de contado"]),
        Definition(7, "coopealianza-loan-pdf-v1", "Prestamo Coopealianza", "pdf", "coopealianza-loan-pdf", ["ver detalles del prestamo", "capital", "interes", "mora", "otros", "total", "saldo"]),
        Definition(8, "bac-account-statement-pdf-v1", "Estado de cuenta consolidado BAC", "pdf", "bac-account-statement-pdf", ["numero de tarjeta", "marca de tarjeta", "plan de lealtad", "pagos vencidos", "pago de contado", "fecha de corte", "total pago de contado"]),
        Definition(9, "bn-card-statement-pdf-v1", "Estado de cuenta de tarjeta Banco Nacional", "pdf", "bn-card-statement-pdf", ["banco nacional de costa rica", "estado de cuenta tarjetas de credito", "detalle de compras del periodo", "total pago de contado"]),
        Definition(10, "bn-debit-csv-v1", "Movimientos de cuenta BN USD", "csv", "bn-debit-csv",
            ["bn-debit-csv-v1"]),  // sentinel: detectado por IBAN en path, no por contenido
        Definition(11, "bn-debit-csv-crc-v1", "Movimientos de cuenta BN CRC", "csv", "bn-debit-csv-crc",
            ["bn-debit-csv-crc-v1"])  // sentinel: detectado por IBAN en path, no por contenido
    ];

    public static IReadOnlyList<ImportTemplate> SeedTemplates() =>
        [.. Definitions.Select(definition => new ImportTemplate
        {
            Id = definition.Id, Code = definition.Code, Name = definition.Name, ContentKind = definition.ContentKind,
            ParserKey = definition.ParserKey, CreatedAt = SeededAt
        })];

    public static IReadOnlyList<ImportTemplatePattern> SeedPatterns() =>
        [.. Definitions.Select((definition, index) =>
        {
            // Sentinel patterns reference their own code as the sole required term — they never match real files
            bool isSentinel = definition.RequiredTerms.Length == 1 && definition.RequiredTerms[0] == definition.Code;
            return new ImportTemplatePattern
            {
                Id = Guid.Parse($"20000000-0000-0000-0000-{index + 1:D12}"), ImportTemplateId = definition.Id,
                PatternKind = "content-terms", RequiredTermsJson = System.Text.Json.JsonSerializer.Serialize(definition.RequiredTerms),
                AlternativeTermGroupsJson = definition.AlternativeTermGroups is null ? null : System.Text.Json.JsonSerializer.Serialize(definition.AlternativeTermGroups),
                IsApproved = !isSentinel, IsActive = !isSentinel, CreatedAt = SeededAt
            };
        })];

    private static ImportTemplateDefinition Definition(int number, string code, string name, string contentKind, string parserKey, string[] requiredTerms, string[][]? alternatives = null, string[]? excludedTerms = null) =>
        new(Guid.Parse($"10000000-0000-0000-0000-{number:D12}"), code, name, contentKind, parserKey, requiredTerms, alternatives, excludedTerms);
}
