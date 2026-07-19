using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;

namespace Bancos.Api.Features.Parsing;

public sealed record ParsedCardMovement(
    DateOnly BookingDate,
    string ExternalReference,
    string Description,
    decimal OriginalAmount,
    string OriginalCurrencyCode,
    decimal? AmountCrc,
    CardOperationKind Operation);

public enum CardOperationKind { Purchase, Payment, Interest, Charge }

/// <summary>Extracts only row-level card movements. Snapshots without dated rows are rejected.</summary>
public sealed class CardStatementParser
{
    private static readonly string[] DateHeaders = ["date", "fecha", "transaction date", "fecha de transaccion", "fecha transaccion"];
    private static readonly string[] DescriptionHeaders = ["description", "descripcion", "detail", "detalle", "concept"];
    private static readonly string[] ReferenceHeaders = ["reference", "referencia", "document", "documento", "transaction id"];
    private static readonly string[] AmountHeaders = ["amount", "monto", "importe", "amount usd", "monto usd", "importe usd"];
    private static readonly string[] AmountCrcHeaders = ["amount crc", "monto crc", "importe crc", "equivalente crc", "equivalente en colones", "monto en colones"];
    private static readonly string[] CurrencyHeaders = ["currency", "moneda"];
    private static readonly string[] OperationHeaders = ["operation", "operacion", "type", "tipo", "transaction type", "tipo de transaccion"];

    public IReadOnlyList<ParsedCardMovement> Parse(ReadOnlyMemory<byte> content)
    {
        var extracted = ImportContentText.Extract(content);
        return extracted.Kind switch
        {
            "csv" => ParseDelimited(extracted.Text, ','),
            "html" => ParseDelimitedRows(HtmlRows(extracted.Text)),
            "xls" => ParseDelimitedRows(SpreadsheetRows(content)),
            "pdf" => ParsePdf(extracted.Text),
            _ => throw new InvalidDataException("El estado de tarjeta no tiene un formato soportado.")
        };
    }

    public IReadOnlyList<ParsedCardMovement> ParseCsv(string csv) => ParseDelimited(csv, ',');

    private static IReadOnlyList<ParsedCardMovement> ParseDelimited(string content, char delimiter) =>
        ParseDelimitedRows(content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(delimiter).Select(value => value.Trim().Trim('"')).ToArray()));

    private static IReadOnlyList<ParsedCardMovement> ParseDelimitedRows(IEnumerable<string[]> source)
    {
        var rows = source.Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value))).ToList();
        var headerIndex = rows.FindIndex(row => FindColumn(row, DateHeaders) >= 0 && FindColumn(row, DescriptionHeaders) >= 0 && FindColumn(row, AmountHeaders) >= 0);
        if (headerIndex < 0) return ParseBacPaymentSummary(rows);

        var header = rows[headerIndex];
        var dateColumn = FindColumn(header, DateHeaders);
        var descriptionColumn = FindColumn(header, DescriptionHeaders);
        var amountColumn = FindColumn(header, AmountHeaders);
        var crcColumn = FindColumn(header, AmountCrcHeaders);
        var currencyColumn = FindColumn(header, CurrencyHeaders);
        var referenceColumn = FindColumn(header, ReferenceHeaders);
        var operationColumn = FindColumn(header, OperationHeaders);
        var movements = new List<ParsedCardMovement>();
        foreach (var row in rows.Skip(headerIndex + 1))
        {
            if (!TryGet(row, dateColumn, out var dateText) || !TryParseDate(dateText, out var date)) continue;
            if (!TryGet(row, descriptionColumn, out var description) || string.IsNullOrWhiteSpace(description)) throw new InvalidDataException("Un movimiento de tarjeta no tiene descripción.");
            if (!TryGet(row, amountColumn, out var amountText) || !TryParseAmount(amountText, out var originalAmount)) throw new InvalidDataException("Un movimiento de tarjeta tiene un importe inválido.");
            var currency = TryGet(row, currencyColumn, out var currencyText) && !string.IsNullOrWhiteSpace(currencyText) ? NormalizeCurrency(currencyText) : InferCurrency(amountText, header[amountColumn]);
            decimal? amountCrc = currency == "CRC" ? originalAmount : TryGet(row, crcColumn, out var crcValue) && TryParseAmount(crcValue, out var parsedCrc) ? parsedCrc : null;
            var reference = TryGet(row, referenceColumn, out var referenceText) && !string.IsNullOrWhiteSpace(referenceText) ? referenceText : $"card-{headerIndex + movements.Count + 1}";
            var operationText = TryGet(row, operationColumn, out var value) ? value : description;
            movements.Add(new ParsedCardMovement(date, reference.Trim(), description.Trim(), originalAmount, currency, amountCrc, InferOperation(operationText)));
        }
        if (movements.Count == 0) throw new InvalidDataException("El estado de tarjeta no contiene movimientos válidos.");
        return movements;
    }

    private static IReadOnlyList<ParsedCardMovement> ParseBacPaymentSummary(IReadOnlyList<string[]> rows)
    {
        var headerIndex = rows.ToList().FindIndex(row =>
            FindColumn(row, ["name"]) >= 0
            && FindColumn(row, ["date"]) >= 0
            && FindColumn(row, ["minimum payment/local amount", "minimum payment/ local amount", "minimum payment local amount"]) >= 0
            && FindColumn(row, ["cash payment/local amount", "cash payment/ local amount", "cash payment local amount"]) >= 0
            && FindColumn(row, ["cash payment/dollar amount", "cash payment / dollar amount", "cash payment dollar amount"]) >= 0);
        if (headerIndex < 0) throw new InvalidDataException("El estado de tarjeta no contiene una tabla de movimientos con fecha, descripción e importe.");
        var header = rows[headerIndex];
        var dueDateColumn = FindColumn(header, ["cash payment/due date", "cash payment due date"]);
        var statementDateColumn = FindColumn(header, DateHeaders);
        var localAmountColumn = FindColumn(header, ["cash payment/local amount", "cash payment/ local amount", "cash payment local amount"]);
        var dollarAmountColumn = FindColumn(header, ["cash payment/dollar amount", "cash payment / dollar amount", "cash payment dollar amount"]);
        var movements = new List<ParsedCardMovement>();
        foreach (var row in rows.Skip(headerIndex + 1))
        {
            var dateValue = TryGet(row, dueDateColumn, out var dueDate) && TryParseDate(dueDate, out var parsedDueDate) ? parsedDueDate : TryGet(row, statementDateColumn, out var statementDate) && TryParseDate(statementDate, out var parsedStatementDate) ? parsedStatementDate : throw new InvalidDataException("El resumen de tarjeta tiene una fecha inválida.");
            AddPayment(row, localAmountColumn, "CRC", dateValue, movements);
            AddPayment(row, dollarAmountColumn, "USD", dateValue, movements);
        }
        if (movements.Count == 0) throw new InvalidDataException("El resumen de tarjeta no contiene importes de pago.");
        return movements;
    }

    private static void AddPayment(string[] row, int amountColumn, string currency, DateOnly date, ICollection<ParsedCardMovement> movements)
    {
        if (!TryGet(row, amountColumn, out var amountText) || !TryParseAmount(amountText, out var amount) || amount == 0m) return;
        // The summary does not expose a transaction description or reference; do not persist cardholder data.
        movements.Add(new ParsedCardMovement(date, $"summary-payment-{movements.Count + 1}", "Pago de tarjeta", amount, currency, currency == "CRC" ? amount : null, CardOperationKind.Payment));
    }

    private static IReadOnlyList<ParsedCardMovement> ParsePdf(string text)
    {
        var movements = new List<ParsedCardMovement>();
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = Regex.Match(line, "^(?<date>\\d{1,2}[/-]\\d{1,2}[/-]\\d{2,4})\\s+(?<description>.+?)\\s+(?<amount>(?:US\\$|USD|₡)?\\s*[+-]?[\\d.,]+)(?:\\s+(?<crc>(?:₡|CRC)\\s*[+-]?[\\d.,]+))?$", RegexOptions.IgnoreCase);
            if (!match.Success || !TryParseDate(match.Groups["date"].Value, out var date) || !TryParseAmount(match.Groups["amount"].Value, out var originalAmount)) continue;
            var amountText = match.Groups["amount"].Value;
            var currency = InferCurrency(amountText, line);
            decimal? amountCrc = currency == "CRC" ? originalAmount : match.Groups["crc"].Success && TryParseAmount(match.Groups["crc"].Value, out var crc) ? crc : null;
            var description = match.Groups["description"].Value.Trim();
            movements.Add(new ParsedCardMovement(date, $"pdf-{movements.Count + 1}", description, originalAmount, currency, amountCrc, InferOperation(description)));
        }
        if (movements.Count == 0) throw new InvalidDataException("El PDF de tarjeta no contiene una tabla de movimientos con fecha e importe.");
        return movements;
    }

    private static IEnumerable<string[]> SpreadsheetRows(ReadOnlyMemory<byte> content)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = new MemoryStream(content.ToArray());
        using var reader = ExcelReaderFactory.CreateReader(stream);
        do
        {
            while (reader.Read())
            {
                var row = new string[reader.FieldCount];
                for (var index = 0; index < reader.FieldCount; index++) row[index] = reader.GetValue(index)?.ToString() ?? string.Empty;
                yield return row;
            }
        } while (reader.NextResult());
    }

    private static IEnumerable<string[]> HtmlRows(string html) => Regex.Matches(html, "<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase)
        .Select(row => Regex.Matches(row.Groups[1].Value, "<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline | RegexOptions.IgnoreCase)
            .Select(cell => Regex.Replace(cell.Groups[1].Value, "<[^>]+>", string.Empty).Trim()).ToArray());

    private static int FindColumn(string[] header, IEnumerable<string> aliases) => Array.FindIndex(header, value => aliases.Contains(ImportTemplateDetector.Normalize(value)));
    private static bool TryGet(string[] row, int index, out string value) { value = index >= 0 && index < row.Length ? row[index] : string.Empty; return index >= 0 && index < row.Length; }
    private static bool TryParseDate(string value, out DateOnly date) => DateOnly.TryParse(value, CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.AllowWhiteSpaces, out date) || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date);
    private static bool TryParseAmount(string value, out decimal amount)
    {
        var normalized = value.Replace("US$", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("USD", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("CRC", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("₡", string.Empty).Trim();
        var style = NumberStyles.Number | NumberStyles.AllowLeadingSign;
        var culture = normalized.Contains('.') && !normalized.Contains(',') ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo("es-CR");
        return decimal.TryParse(normalized, style, culture, out amount) || decimal.TryParse(normalized, style, culture == CultureInfo.InvariantCulture ? CultureInfo.GetCultureInfo("es-CR") : CultureInfo.InvariantCulture, out amount);
    }
    private static string NormalizeCurrency(string value) => value.Contains("USD", StringComparison.OrdinalIgnoreCase) || value.Contains("US$", StringComparison.OrdinalIgnoreCase) || value.Contains('$') ? "USD" : "CRC";
    private static string InferCurrency(string amount, string context) => NormalizeCurrency(amount + " " + context);
    private static CardOperationKind InferOperation(string value)
    {
        var normalized = ImportTemplateDetector.Normalize(value);
        if (normalized.Contains("pago") || normalized.Contains("payment")) return CardOperationKind.Payment;
        if (normalized.Contains("interes") || normalized.Contains("interest")) return CardOperationKind.Interest;
        if (normalized.Contains("cargo") || normalized.Contains("comision") || normalized.Contains("fee") || normalized.Contains("charge")) return CardOperationKind.Charge;
        return CardOperationKind.Purchase;
    }
}
