using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;

namespace Bancos.Mcp.Features.Parsing;

public sealed class BacCreditFinancingXlsParser
{
    private static readonly string[] RequiredHeaders = ["fecha", "concepto", "cuotas", "monto de cuota", "saldo inicial", "saldo faltante"];
    private static readonly Regex IdentifierPattern = new(@"(?<!\d)(?:\d[\s-]?){7,18}\d(?!\d)", RegexOptions.Compiled);

    public static ISet<string> ExtractIdentifierFingerprints(ReadOnlyMemory<byte> content)
    {
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        using var reader = CreateReader(content);
        foreach (var cell in ReadRows(reader).SelectMany(row => row))
        {
            foreach (Match match in IdentifierPattern.Matches(cell))
            {
                var identifier = string.Concat(match.Value.Where(char.IsDigit));
                fingerprints.Add(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identifier))));
            }
        }

        return fingerprints;
    }

    public IReadOnlyList<ParsedCreditFinancing> Parse(ReadOnlyMemory<byte> content)
    {
        using var reader = CreateReader(content);
        var rows = ReadRows(reader);
        var headerIndex = rows.FindIndex(row => RequiredHeaders.All(header => row.Any(cell => TextNormalizer.Normalize(cell) == header)));
        if (headerIndex < 0) throw new InvalidDataException("BAC financing XLS does not contain the required header row.");

        var columns = rows[headerIndex]
            .Select((value, index) => (Header: TextNormalizer.Normalize(value), Index: index))
            .Where(x => RequiredHeaders.Contains(x.Header))
            .ToDictionary(x => x.Header, x => x.Index);
        var financings = new List<ParsedCreditFinancing>();
        foreach (var row in rows.Skip(headerIndex + 1).Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value))))
        {
            var values = RequiredHeaders.ToDictionary(header => header, header => GetCell(row, columns[header]));
            if (values.Values.All(string.IsNullOrWhiteSpace)) continue;
            if (!TryParseDate(values["fecha"], out _)) continue;
            if (values.Values.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Each BAC financing row must include every required value.");
            financings.Add(new ParsedCreditFinancing(
                ParseDate(values["fecha"]),
                values["concepto"].Trim(),
                values["cuotas"].Trim(),
                ParseAmount(values["monto de cuota"], "monto de cuota"),
                ParseAmount(values["saldo inicial"], "saldo inicial"),
                ParseAmount(values["saldo faltante"], "saldo faltante"),
                ExtractCurrency(values["monto de cuota"])));
        }
        if (financings.Count == 0) throw new InvalidDataException("BAC financing XLS does not contain financing rows.");
        return financings;
    }

    private static List<string[]> ReadRows(IExcelDataReader reader)
    {
        var rows = new List<string[]>();
        do
        {
            while (reader.Read())
            {
                var row = new string[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++) row[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
                rows.Add(row);
            }
        } while (reader.NextResult());
        return rows;
    }

    private static IExcelDataReader CreateReader(ReadOnlyMemory<byte> content)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return ExcelReaderFactory.CreateReader(new MemoryStream(content.ToArray()));
    }

    private static string GetCell(string[] row, int index) => index < row.Length ? row[index] : string.Empty;
    private static DateOnly ParseDate(string value)
    {
        var datePart = value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
        return DateOnly.TryParse(datePart, CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.AllowWhiteSpaces, out var result) || DateOnly.TryParse(datePart, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result)
        ? result : throw new InvalidDataException($"Invalid BAC financing date '{value}'.");
    }
    private static bool TryParseDate(string value, out DateOnly result)
    {
        var datePart = value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        return DateOnly.TryParse(datePart, CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.AllowWhiteSpaces, out result)
            || DateOnly.TryParse(datePart, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result);
    }
    private static decimal ParseAmount(string value, string field)
    {
        if (MoneyParser.TryParse(value, out var result)) return result;
        throw new InvalidDataException($"Invalid BAC financing {field} '{value}'.");
    }
    private static string ExtractCurrency(string value)
    {
        var upper = value.ToUpperInvariant();
        if (upper.Contains("USD") || upper.Contains("US$")) return "USD";
        return "CRC";
    }
}
