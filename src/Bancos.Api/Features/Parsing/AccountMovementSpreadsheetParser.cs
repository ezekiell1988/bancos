using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;

namespace Bancos.Api.Features.Parsing;

/// <summary>Reads account-movement spreadsheets by their documented column labels, never their position.</summary>
public sealed class AccountMovementSpreadsheetParser
{
    private static readonly string[] DateHeaders = ["fecha", "fecha movimiento", "fecha de movimiento"];
    private static readonly string[] ReferenceHeaders = ["documento", "numero documento", "referencia", "numero"];
    private static readonly string[] DescriptionHeaders = ["descripcion", "detalle", "concepto"];
    private static readonly string[] DebitHeaders = ["debito", "debitos"];
    private static readonly string[] CreditHeaders = ["credito", "creditos"];

    public IReadOnlyList<ParsedBankMovement> Parse(ReadOnlyMemory<byte> content)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = content.ToArray();
        var text = Encoding.UTF8.GetString(bytes);
        var rows = text.Contains("<table", StringComparison.OrdinalIgnoreCase) ? ReadHtmlRows(text) : ReadSpreadsheetRows(bytes);
        var headerIndex = rows.FindIndex(row => HasAny(row, DateHeaders) && HasAny(row, DescriptionHeaders) && (HasAny(row, DebitHeaders) || HasAny(row, CreditHeaders)));
        if (headerIndex < 0) throw new InvalidDataException("La hoja no contiene encabezados de movimientos reconocibles.");
        var columns = rows[headerIndex].Select((value, index) => (Header: ImportTemplateDetector.Normalize(value), index)).ToArray();
        var movements = new List<ParsedBankMovement>();
        foreach (var row in rows.Skip(headerIndex + 1))
        {
            var date = Get(row, columns, DateHeaders); var description = Get(row, columns, DescriptionHeaders);
            if (string.IsNullOrWhiteSpace(date) && string.IsNullOrWhiteSpace(description)) continue;
            if (!DateOnly.TryParse(date, CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.AllowWhiteSpaces, out var bookingDate)) continue;
            var debit = Amount(Get(row, columns, DebitHeaders)); var credit = Amount(Get(row, columns, CreditHeaders));
            if ((debit == 0m) == (credit == 0m)) throw new InvalidDataException("Cada movimiento debe tener un único débito o crédito.");
            movements.Add(new ParsedBankMovement(bookingDate, Get(row, columns, ReferenceHeaders), description.Trim(), debit, credit));
        }
        if (movements.Count == 0) throw new InvalidDataException("La hoja no contiene movimientos válidos.");
        return movements;
    }

    private static List<string[]> ReadSpreadsheetRows(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var rows = new List<string[]>();
        do { while (reader.Read()) rows.Add(Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i)?.ToString() ?? string.Empty).ToArray()); } while (reader.NextResult());
        return rows;
    }

    private static List<string[]> ReadHtmlRows(string html) => Regex.Matches(html, "<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        .Select(row => Regex.Matches(row.Groups[1].Value, "<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            .Select(cell => NormalizeHtmlCell(cell.Groups[1].Value)).ToArray())
        .Where(row => row.Length > 0)
        .ToList();

    private static string NormalizeHtmlCell(string value)
    {
        var withoutTags = Regex.Replace(value, "<[^>]+>", " ", RegexOptions.CultureInvariant);
        return Regex.Replace(WebUtility.HtmlDecode(withoutTags).Replace('\u00a0', ' '), "\\s+", " ", RegexOptions.CultureInvariant).Trim();
    }
    private static bool HasAny(string[] row, string[] labels) => row.Select(ImportTemplateDetector.Normalize).Any(value => labels.Contains(value));
    private static string Get(string[] row, (string Header, int index)[] columns, string[] labels) { var column = columns.FirstOrDefault(x => labels.Contains(x.Header)); return column == default || column.index >= row.Length ? string.Empty : row[column.index].Trim(); }
    private static decimal Amount(string value) { var cleaned = value.Replace("₡", string.Empty).Replace("CRC", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(); return string.IsNullOrEmpty(cleaned) ? 0m : decimal.Parse(cleaned, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.GetCultureInfo("es-CR")); }
}
