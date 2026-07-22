using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;

namespace Bancos.Mcp.Features.Parsing;

public sealed class CardStatementParser
{
    private static readonly string[] DateHeaders = ["date", "fecha", "transaction date", "fecha de transaccion", "fecha transaccion"];
    private static readonly string[] DescriptionHeaders = ["description", "descripcion", "detail", "detalle", "concept"];
    private static readonly string[] ReferenceHeaders = ["reference", "referencia", "document", "documento", "transaction id"];
    private static readonly string[] AmountHeaders = ["amount", "monto", "importe", "amount usd", "monto usd", "importe usd"];
    private static readonly string[] AmountCrcHeaders = ["amount crc", "monto crc", "importe crc", "equivalente crc", "equivalente en colones", "monto en colones"];
    private static readonly string[] AmountLocalHeaders = ["local"];
    private static readonly string[] AmountDollarsHeaders = ["dollars", "dolares"];
    private static readonly string[] CurrencyHeaders = ["currency", "moneda"];
    private static readonly string[] OperationHeaders = ["operation", "operacion", "type", "tipo", "transaction type", "tipo de transaccion"];

    public ParsedCardStatement Parse(ReadOnlyMemory<byte> content)
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

    private static ParsedCardStatement ParseDelimited(string content, char delimiter) =>
        ParseDelimitedRows(content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(delimiter).Select(value => value.Trim().Trim('"')).ToArray()));

    private static ParsedCardStatement ParseDelimitedRows(IEnumerable<string[]> source)
    {
        var rows = source.Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value))).ToList();
        var headerIndex = rows.FindIndex(row => FindColumn(row, DateHeaders) >= 0 && FindColumn(row, DescriptionHeaders) >= 0 && FindColumn(row, AmountHeaders) >= 0);
        if (headerIndex < 0)
        {
            if (HasBacPaymentSummary(rows))
                return new ParsedCardStatement(CardStatementContentKind.PaymentSummary, []);

            var bacIdx = rows.FindIndex(row => FindColumn(row, DateHeaders) >= 0 && FindColumn(row, AmountLocalHeaders) >= 0 && FindColumn(row, AmountDollarsHeaders) >= 0);
            if (bacIdx >= 0)
                return ParseBacDualAmountRows(rows, bacIdx);

            throw new InvalidDataException("El estado de tarjeta no contiene una tabla de movimientos con fecha, descripción e importe.");
        }

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
        return new ParsedCardStatement(CardStatementContentKind.Movements, movements);
    }

    private static ParsedCardStatement ParseBacDualAmountRows(List<string[]> rows, int headerIndex)
    {
        var header = rows[headerIndex];
        var dateColumn = FindColumn(header, DateHeaders);
        var localColumn = FindColumn(header, AmountLocalHeaders);
        var dollarsColumn = FindColumn(header, AmountDollarsHeaders);
        var descriptionColumn = Enumerable.Range(0, header.Length).FirstOrDefault(i => i != dateColumn && i != localColumn && i != dollarsColumn, -1);
        var movements = new List<ParsedCardMovement>();
        foreach (var row in rows.Skip(headerIndex + 1))
        {
            if (!TryGet(row, dateColumn, out var dateText) || !TryParseDate(dateText, out var date)) continue;
            TryGet(row, localColumn, out var localText);
            TryGet(row, dollarsColumn, out var dollarsText);
            var hasLocal = TryParseAmount(localText, out var localAmount) && localAmount != 0;
            var hasDollars = TryParseAmount(dollarsText, out var dollarsAmount) && dollarsAmount != 0;
            if (!hasLocal && !hasDollars) continue;
            TryGet(row, descriptionColumn, out var description);
            if (string.IsNullOrWhiteSpace(description)) throw new InvalidDataException("Un movimiento de tarjeta no tiene descripción.");
            var (originalAmount, currency, amountCrc) = hasLocal
                ? (localAmount, "CRC", (decimal?)localAmount)
                : (dollarsAmount, "USD", (decimal?)null);
            movements.Add(new ParsedCardMovement(date, $"card-{movements.Count + 1}", description.Trim(), originalAmount, currency, amountCrc, InferOperation(description)));
        }
        if (movements.Count == 0) throw new InvalidDataException("El estado de tarjeta no contiene movimientos válidos.");
        return new ParsedCardStatement(CardStatementContentKind.Movements, movements);
    }

    private static bool HasBacPaymentSummary(IReadOnlyList<string[]> rows) =>
        rows.Any(row =>
            FindColumn(row, DateHeaders) >= 0
            && FindColumn(row, ["cash payment/local amount", "cash payment/ local amount", "cash payment local amount"]) >= 0
            && FindColumn(row, ["cash payment/dollar amount", "cash payment / dollar amount", "cash payment dollar amount"]) >= 0);

    internal static ParsedCardStatement ParsePdf(string text)
    {
        var movements = ParsePdfLines(text);
        if (movements.Count == 0)
            movements = ParseBacOnlinePdfConcatenated(text);

        if (movements.Count > 0)
            return new ParsedCardStatement(CardStatementContentKind.Movements, movements);

        var normalized = TextNormalizer.Normalize(text);
        if (normalized.Contains("tarjeta de credito")
            && normalized.Contains("saldo en colones")
            && normalized.Contains("saldo en dolares")
            && (normalized.Contains("pago de tarjeta al dia") || normalized.Contains("fecha de pago de contado")))
            return new ParsedCardStatement(CardStatementContentKind.BalanceSnapshot, []);

        throw new InvalidDataException("El PDF de tarjeta no contiene una tabla de movimientos con fecha e importe.");
    }

    private static List<ParsedCardMovement> ParsePdfLines(string text)
    {
        var movements = new List<ParsedCardMovement>();
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = Regex.Match(line, "^(?<date>\\d{1,2}(?:[/-]\\d{1,2}[/-]\\d{2,4}|\\s+(?:ene|feb|mar|abr|may|jun|jul|ago|sep|oct|nov|dic)[a-z.]*\\s+\\d{2,4}))\\s+(?<description>.+?)\\s+(?<amount>(?:US\\$|USD|₡)?\\s*[+-]?[\\d.,]+)(?:\\s+(?<crc>(?:₡|CRC)\\s*[+-]?[\\d.,]+)|\\s+(?<trailingcurrency>[A-Z]{3}))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success || !TryParseDate(match.Groups["date"].Value, out var date) || !TryParseAmount(match.Groups["amount"].Value, out var originalAmount)) continue;
            var amountText = match.Groups["amount"].Value;
            var trailingCurrency = match.Groups["trailingcurrency"].Success ? match.Groups["trailingcurrency"].Value : null;
            var currency = trailingCurrency is not null ? NormalizeCurrency(trailingCurrency) : InferCurrency(amountText, line);
            decimal? amountCrc = currency == "CRC" ? originalAmount : match.Groups["crc"].Success && TryParseAmount(match.Groups["crc"].Value, out var crc) ? crc : null;
            var description = match.Groups["description"].Value.Trim();
            movements.Add(new ParsedCardMovement(date, $"pdf-{movements.Count + 1}", description, originalAmount, currency, amountCrc, InferOperation(description)));
        }
        return movements;
    }

    private static List<ParsedCardMovement> ParseBacOnlinePdfConcatenated(string text)
    {
        var movements = new List<ParsedCardMovement>();
        var normalized = TextNormalizer.Normalize(text);
        var tableMarker = "fechaconceptomonto colones";
        var markerIndex = normalized.IndexOf(tableMarker, StringComparison.Ordinal);
        if (markerIndex < 0) return movements;
        var body = text[(markerIndex + tableMarker.Length)..];

        var matches = Regex.Matches(body,
            "(?<date>\\d{2}/\\d{2}/\\d{4})(?<description>.+?)(?<amount>[\\d,]+\\.\\d{2})\\s*(?<currency>CRC|USD)(?=\\d{2}/\\d{2}/\\d{4}|Los pagos|Movimientos|$)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        foreach (Match m in matches)
        {
            if (!TryParseDate(m.Groups["date"].Value, out var date)) continue;
            if (!TryParseAmount(m.Groups["amount"].Value, out var amount)) continue;
            var description = m.Groups["description"].Value.Trim().TrimEnd('\\').Trim();
            var currency = NormalizeCurrency(m.Groups["currency"].Value);
            decimal? amountCrc = currency == "CRC" ? amount : null;
            movements.Add(new ParsedCardMovement(date, $"pdf-{movements.Count + 1}", description, amount, currency, amountCrc, InferOperation(description)));
        }
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

    private static int FindColumn(string[] header, IEnumerable<string> aliases) => Array.FindIndex(header, value => aliases.Contains(TextNormalizer.Normalize(value)));
    private static bool TryGet(string[] row, int index, out string value) { value = index >= 0 && index < row.Length ? row[index] : string.Empty; return index >= 0 && index < row.Length; }
    private static bool TryParseDate(string value, out DateOnly date) => DateOnly.TryParse(value, CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.AllowWhiteSpaces, out date) || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date);
    private static bool TryParseAmount(string value, out decimal amount) => MoneyParser.TryParse(value, out amount);
    private static string NormalizeCurrency(string value) => value.Contains("USD", StringComparison.OrdinalIgnoreCase) || value.Contains("US$", StringComparison.OrdinalIgnoreCase) || value.Contains('$') ? "USD" : "CRC";
    private static string InferCurrency(string amount, string context) => NormalizeCurrency(amount + " " + context);
    internal static CardOperationKind InferOperation(string value)
    {
        var normalized = TextNormalizer.Normalize(value);
        if (normalized.Contains("pago") || normalized.Contains("payment")) return CardOperationKind.Payment;
        if (normalized.Contains("interes") || normalized.Contains("interest")) return CardOperationKind.Interest;
        if (normalized.Contains("cargo") || normalized.Contains("comision") || normalized.Contains("fee") || normalized.Contains("charge")) return CardOperationKind.Charge;
        return CardOperationKind.Purchase;
    }
}
