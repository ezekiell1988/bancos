using System.Globalization;
using System.Text.RegularExpressions;

namespace Bancos.Mcp.Features.Parsing;

public sealed partial class BacAccountStatementPdfParser
{
    private static readonly Dictionary<string, int> SpanishMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ENE"] = 1, ["FEB"] = 2, ["MAR"] = 3, ["ABR"] = 4, ["MAY"] = 5, ["JUN"] = 6,
        ["JUL"] = 7, ["AGO"] = 8, ["SEP"] = 9, ["OCT"] = 10, ["NOV"] = 11, ["DIC"] = 12
    };

    public IReadOnlyList<ParsedBacAccountStatement> Parse(ReadOnlyMemory<byte> content)
    {
        var extracted = ImportContentText.Extract(content);
        if (extracted.Kind != "pdf") throw new InvalidDataException("El estado de cuenta consolidado BAC debe ser un PDF.");
        return ParseText(extracted.Text);
    }

    internal static IReadOnlyList<ParsedBacAccountStatement> ParseText(string text)
    {
        var normalized = TextNormalizer.Normalize(text);
        if (!normalized.Contains("numero de tarjeta") || !normalized.Contains("pago de contado") || !normalized.Contains("total pago de contado"))
            throw new InvalidDataException("El PDF no contiene la firma del estado de cuenta consolidado BAC.");

        var sections = CardSectionSplit().Split(text)
            .Where(s => s.Contains("Número de cuenta:") && s.Contains("Total pago de contado"))
            .ToArray();

        if (sections.Length == 0)
            throw new InvalidDataException("El estado de cuenta BAC no contiene secciones de tarjeta reconocibles.");

        var results = new List<ParsedBacAccountStatement>();
        foreach (var section in sections)
        {
            var cardNumber = Extract(CardNumberRegex(), section, "número de cuenta");
            var brand = Extract(BrandRegex(), section, "marca de tarjeta");
            var plan = Extract(PlanRegex(), section, "plan de lealtad");
            var statementDate = ParseBacDate(Extract(StatementDateRegex(), section, "fecha de corte"), "fecha de corte");
            var paymentDue = ParseBacDate(Extract(PaymentDueDateRegex(), section, "fecha límite pago de contado"), "fecha límite pago de contado");

            var minCrcMatch = MinimumCrcRegex().Match(section);
            var cashMatch = CashPaymentRegex().Match(section);
            if (!minCrcMatch.Success || !cashMatch.Success)
                throw new InvalidDataException($"No se encontraron montos de pago para la tarjeta {cardNumber}.");

            var minUsd = 0m;
            var minUsdMatch = MinimumUsdRegex().Match(section);
            if (minUsdMatch.Success) MoneyParser.TryParse(minUsdMatch.Groups["amount"].Value, out minUsd);

            var minCrc = ParseAmount(minCrcMatch.Groups["amount"].Value, "pago mínimo colones");
            var cashCrc = ParseAmount(cashMatch.Groups["crc"].Value, "pago contado colones");
            var cashUsd = ParseAmount(cashMatch.Groups["usd"].Value, "pago contado dólares");

            results.Add(new ParsedBacAccountStatement(cardNumber.Trim(), brand.Trim(), plan.Trim(), statementDate, paymentDue, minCrc, minUsd, cashCrc, cashUsd));
        }
        return results;
    }

    private static string Extract(Regex regex, string text, string field)
    {
        var m = regex.Match(text);
        return m.Success ? m.Groups["value"].Value.Trim() : throw new InvalidDataException($"Campo '{field}' no encontrado en sección de tarjeta.");
    }

    private static DateOnly ParseBacDate(string value, string field)
    {
        var parts = value.Trim().Split('-');
        if (parts.Length == 3 && int.TryParse(parts[0], out var day) && SpanishMonths.TryGetValue(parts[1], out var month) && int.TryParse(parts[2], out var year))
            return new DateOnly(2000 + year, month, day);
        throw new InvalidDataException($"Fecha inválida '{value}' en campo '{field}'.");
    }

    private static decimal ParseAmount(string value, string field)
    {
        if (MoneyParser.TryParse(value, out var result)) return result;
        throw new InvalidDataException($"Monto inválido '{value}' en campo '{field}'.");
    }

    [GeneratedRegex(@"(?=Marca de tarjeta:)")]
    private static partial Regex CardSectionSplit();

    [GeneratedRegex(@"Número de cuenta:\s*(?<value>[*\d]+)")]
    private static partial Regex CardNumberRegex();

    [GeneratedRegex(@"Marca de tarjeta:(?<value>.+?)(?=Número de cuenta:)")]
    private static partial Regex BrandRegex();

    [GeneratedRegex(@"Plan de lealtad:(?<value>.+?)(?=Cuenta IBAN|Fecha de emisión|Fecha vencimiento)")]
    private static partial Regex PlanRegex();

    [GeneratedRegex(@"Fecha de corte:(?<value>\d+-[A-Z]+-\d+)")]
    private static partial Regex StatementDateRegex();

    [GeneratedRegex(@"Fecha límite pago de contado:(?<value>\d+-[A-Z]+-\d+)")]
    private static partial Regex PaymentDueDateRegex();

    [GeneratedRegex(@"Total pago m[ií]nimo(?<amount>[\d,]+\.\d{2})[\d,]+\.\d{2}")]
    private static partial Regex MinimumCrcRegex();

    [GeneratedRegex(@"Pago M[ií]nimo en D[oó]lares(?<amount>[\d,]+\.\d{2})")]
    private static partial Regex MinimumUsdRegex();

    [GeneratedRegex(@"Total pago de contado(?<crc>[\d,]+\.\d{2})(?<usd>[\d,]+\.\d{2})")]
    private static partial Regex CashPaymentRegex();
}
