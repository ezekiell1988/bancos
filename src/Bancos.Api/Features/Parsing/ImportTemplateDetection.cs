using System.Text;
using ExcelDataReader;
using UglyToad.PdfPig;

namespace Bancos.Api.Features.Parsing;

public static class ImportTemplates
{
    public const string BcrDebitCsvV1 = "bcr-debit-csv-v1";
    public const string BacCreditCsvV1 = "bac-credit-csv-v1";
    public const string BcrDebitHtmlXlsV1 = "bcr-debit-html-xls-v1";
    public const string BacCreditFinancingXlsV1 = "bac-credit-financing-xls-v1";
    public const string BacCreditOnlinePdfV1 = "bac-credit-online-pdf-v1";
    public const string CoopealianzaLoanPdfV1 = "coopealianza-loan-pdf-v1";
    public const string Unknown = "unknown";
}

public sealed record ImportTemplateDetection(string Template, string ContentKind, IReadOnlyList<string> Evidence)
{
    public bool IsKnown => Template != ImportTemplates.Unknown;
}

/// <summary>Identifies a documented bank template exclusively from content signatures.</summary>
public sealed class ImportTemplateDetector
{
    public ImportTemplateDetection Detect(ReadOnlyMemory<byte> content)
    {
        var extracted = ImportContentText.Extract(content);
        return Detect(extracted.Text, extracted.Kind);
    }

    public static ImportTemplateDetection Detect(string text, string contentKind)
    {
        var normalized = Normalize(text);
        var matches = new List<(string Template, string[] Evidence)>();
        if (contentKind == "csv" && ContainsAll(normalized, ";", "oficina", "fechamovimiento", "numerodocumento", "debito", "credito", "descripcion"))
            matches.Add((ImportTemplates.BcrDebitCsvV1, ["delimitador ;", "encabezados de movimientos BCR"]));
        if (contentKind == "csv" && ContainsAll(normalized, ",", "product", "name", "date") && (normalized.Contains("pago minimo") || normalized.Contains("pago contado")))
            matches.Add((ImportTemplates.BacCreditCsvV1, ["encabezados BAC", "campos de pago"]));
        if (contentKind == "html" && ContainsAll(normalized, "banco de costa rica", "movimientos por rango de fechas"))
            matches.Add((ImportTemplates.BcrDebitHtmlXlsV1, ["Banco de Costa Rica", "Movimientos por rango de fechas"]));
        if (contentKind == "xls" && ContainsAll(normalized, "consulta de financiamientos", "fecha", "concepto", "cuotas", "monto de cuota", "saldo inicial", "saldo faltante"))
            matches.Add((ImportTemplates.BacCreditFinancingXlsV1, ["encabezados de financiamientos BAC"]));
        if (contentKind == "pdf" && ContainsAll(normalized, "tarjeta de credito", "saldo en colones", "saldo en dolares", "pago de tarjeta al dia"))
            matches.Add((ImportTemplates.BacCreditOnlinePdfV1, ["snapshot de tarjeta BAC"]));
        if (contentKind == "pdf" && ContainsAll(normalized, "ver detalles del prestamo", "capital", "interes", "mora", "otros", "total", "saldo"))
            matches.Add((ImportTemplates.CoopealianzaLoanPdfV1, ["tabla de préstamo Coopealianza"]));

        return matches.Count == 1
            ? new ImportTemplateDetection(matches[0].Template, contentKind, matches[0].Evidence)
            : new ImportTemplateDetection(ImportTemplates.Unknown, contentKind, matches.Count == 0 ? ["sin firma documentada"] : ["firmas ambiguas"]);
    }

    private static bool ContainsAll(string text, params string[] values) => values.All(text.Contains);
    internal static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)).ToLowerInvariant();
}

public static class ImportContentText
{
    public static (string Kind, string Text) Extract(ReadOnlyMemory<byte> content)
    {
        var bytes = content.ToArray();
        if (bytes.AsSpan().StartsWith("%PDF-"u8)) return ("pdf", ExtractPdf(bytes));
        if (bytes.AsSpan().StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0 })) return ("xls", ExtractXls(bytes));
        var text = Encoding.UTF8.GetString(bytes);
        if (text.Contains("<html", StringComparison.OrdinalIgnoreCase) || text.Contains("<table", StringComparison.OrdinalIgnoreCase)) return ("html", text);
        return ("csv", text);
    }

    private static string ExtractPdf(byte[] bytes)
    {
        using var document = PdfDocument.Open(bytes);
        return string.Join('\n', document.GetPages().Select(page => page.Text));
    }

    private static string ExtractXls(byte[] bytes)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var stream = new MemoryStream(bytes);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var values = new List<string>();
        do { while (reader.Read()) for (var column = 0; column < reader.FieldCount; column++) if (reader.GetValue(column) is { } value) values.Add(value.ToString()!); } while (reader.NextResult());
        return string.Join('\n', values);
    }
}
