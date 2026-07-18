using System.Globalization;
using System.Text;
using ExcelDataReader;

namespace Bancos.Api.Features.Parsing;

public sealed record ParsedCreditFinancing(DateOnly FinancingDate, string Concept, string Installments, decimal InstallmentAmount, decimal InitialBalance, decimal OutstandingBalance);

/// <summary>Parses the documented BAC financing XLS template from its cell values.</summary>
public sealed class BacCreditFinancingXlsParser
{
    private static readonly string[] RequiredHeaders = ["fecha", "concepto", "cuotas", "monto de cuota", "saldo inicial", "saldo faltante"];

    public IReadOnlyList<ParsedCreditFinancing> Parse(ReadOnlyMemory<byte> content)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = new MemoryStream(content.ToArray());
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var rows = ReadRows(reader);
        var headerIndex = rows.FindIndex(row => RequiredHeaders.All(header => row.Any(cell => ImportTemplateDetector.Normalize(cell) == header)));
        if (headerIndex < 0) throw new InvalidDataException("BAC financing XLS does not contain the required header row.");

        var columns = rows[headerIndex]
            .Select((value, index) => (Header: ImportTemplateDetector.Normalize(value), Index: index))
            .Where(x => RequiredHeaders.Contains(x.Header))
            .ToDictionary(x => x.Header, x => x.Index);
        var financings = new List<ParsedCreditFinancing>();
        foreach (var row in rows.Skip(headerIndex + 1).Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value))))
        {
            var values = RequiredHeaders.ToDictionary(header => header, header => GetCell(row, columns[header]));
            if (values.Values.All(string.IsNullOrWhiteSpace)) continue;
            if (values.Values.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Each BAC financing row must include every required value.");
            financings.Add(new ParsedCreditFinancing(
                ParseDate(values["fecha"]),
                values["concepto"].Trim(),
                values["cuotas"].Trim(),
                ParseAmount(values["monto de cuota"], "monto de cuota"),
                ParseAmount(values["saldo inicial"], "saldo inicial"),
                ParseAmount(values["saldo faltante"], "saldo faltante")));
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

    private static string GetCell(string[] row, int index) => index < row.Length ? row[index] : string.Empty;
    private static DateOnly ParseDate(string value)
    {
        var datePart = value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
        return DateOnly.TryParse(datePart, CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.AllowWhiteSpaces, out var result) || DateOnly.TryParse(datePart, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result)
        ? result : throw new InvalidDataException($"Invalid BAC financing date '{value}'.");
    }
    private static decimal ParseAmount(string value, string field)
    {
        var normalized = value.Replace("₡", string.Empty).Replace("CRC", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        if (decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.GetCultureInfo("es-CR"), out var result) || decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out result)) return result;
        throw new InvalidDataException($"Invalid BAC financing {field} '{value}'.");
    }
}
