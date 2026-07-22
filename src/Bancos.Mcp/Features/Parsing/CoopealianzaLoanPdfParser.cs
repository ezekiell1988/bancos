using System.Globalization;
using System.Text.RegularExpressions;

namespace Bancos.Mcp.Features.Parsing;

public sealed partial class CoopealianzaLoanPdfParser
{
    private const decimal ReconciliationTolerance = 0.01m;

    public ParsedCoopealianzaLoan Parse(ReadOnlyMemory<byte> content)
    {
        var extracted = ImportContentText.Extract(content);
        if (extracted.Kind != "pdf") throw new InvalidDataException("Coopealianza loans must be imported from a PDF.");
        return ParseText(extracted.Text);
    }

    internal static ParsedCoopealianzaLoan ParseText(string text)
    {
        var normalized = TextNormalizer.Normalize(text);
        if (!ContainsRequiredSignature(normalized)) throw new InvalidDataException("PDF does not contain the documented Coopealianza loan signature.");

        var originalAmount = ParseHeaderAmount(OriginalAmountRegex().Match(text), "monto original");
        var interestRate = ParseHeaderRate(InterestRateRegex().Match(text));
        var termMonths = ParseHeaderTerm(TermRegex().Match(text));
        var startDate = ParseHeaderDate(StartDateRegex().Match(text));
        var outstandingBalance = ParseHeaderAmount(BalanceRegex().Match(text), "saldo actual");

        var payments = PaymentRegex().Matches(text)
            .Select(match => new ParsedCoopealianzaLoanPayment(
                ParseDate(match.Groups["date"].Value),
                ParseAmount(match.Groups["capital"].Value, "capital"),
                ParseAmount(match.Groups["interest"].Value, "interest"),
                ParseAmount(match.Groups["lateFee"].Value, "mora"),
                ParseAmount(match.Groups["other"].Value, "otros"),
                ParseAmount(match.Groups["total"].Value, "total")))
            .ToArray();

        foreach (var payment in payments)
        {
            var components = payment.Capital + payment.Interest + payment.LateFee + payment.OtherCharges;
            if (Math.Abs(components - payment.Total) > ReconciliationTolerance)
                throw new InvalidDataException($"Coopealianza payment on {payment.PaymentDate:yyyy-MM-dd} does not reconcile to its total.");
        }

        var cuotas = CuotaRegex().Matches(text)
            .Select(match => new ParsedCoopealianzaLoanCuota(
                int.Parse(match.Groups["num"].Value),
                ParseDate(match.Groups["date"].Value),
                ParseAmount(match.Groups["balance"].Value, "saldo cuota"),
                ParseAmount(match.Groups["capital"].Value, "capital cuota"),
                ParseAmount(match.Groups["interest"].Value, "interes cuota"),
                ParseAmount(match.Groups["lateFee"].Value, "mora cuota"),
                ParseAmount(match.Groups["other"].Value, "otros cuota"),
                ParseAmount(match.Groups["total"].Value, "total cuota"),
                match.Groups["status"].Value.Trim()))
            .OrderBy(c => c.CuotaNumber)
            .ToArray();

        return new ParsedCoopealianzaLoan(originalAmount, interestRate, termMonths, startDate, outstandingBalance, payments, cuotas);
    }

    private static bool ContainsRequiredSignature(string text) => new[] { "ver detalles del prestamo", "capital", "interes", "mora", "otros", "total", "saldo" }.All(text.Contains);

    private static DateOnly ParseDate(string value) => DateOnly.TryParseExact(value, ["dd/MM/yyyy", "d/M/yyyy"], CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.None, out var result)
        ? result : throw new InvalidDataException($"Invalid Coopealianza payment date '{value}'.");

    private static decimal ParseAmount(string value, string field)
    {
        if (MoneyParser.TryParse(value, out var result)) return result;
        throw new InvalidDataException($"Invalid Coopealianza loan {field} '{value}'.");
    }

    private static decimal ParseHeaderAmount(Match match, string field)
    {
        if (!match.Success) throw new InvalidDataException($"Coopealianza loan PDF does not contain {field}.");
        return ParseAmount(match.Groups["value"].Value, field);
    }

    private static decimal ParseHeaderRate(Match match)
    {
        if (!match.Success) throw new InvalidDataException("Coopealianza loan PDF does not contain interest rate.");
        var raw = match.Groups["value"].Value.Replace(",", ".").Trim();
        return decimal.Parse(raw, CultureInfo.InvariantCulture);
    }

    private static int ParseHeaderTerm(Match match)
    {
        if (!match.Success) throw new InvalidDataException("Coopealianza loan PDF does not contain term.");
        return int.Parse(match.Groups["total"].Value);
    }

    private static DateOnly ParseHeaderDate(Match match)
    {
        if (!match.Success) throw new InvalidDataException("Coopealianza loan PDF does not contain start date.");
        return ParseDate(match.Groups["date"].Value);
    }

    [GeneratedRegex(@"(?i)Monto original\s*₡\s*(?<value>[\d][\d.,\s]*)")]
    private static partial Regex OriginalAmountRegex();

    [GeneratedRegex(@"(?i)Tasa\s+(?<value>[\d]+[.,]?\d*)\s*%")]
    private static partial Regex InterestRateRegex();

    [GeneratedRegex(@"(?i)(?:Cuota actual|En)\s*[:\s]*\d+\s*/\s*(?<total>\d+)")]
    private static partial Regex TermRegex();

    [GeneratedRegex(@"(?i)(?:Inicio|FECHA\s+TASA)\s+(?<date>\d{2}/\d{2}/\d{4})\s+\d+[.,]\d+\s*%")]
    private static partial Regex StartDateRegex();

    [GeneratedRegex(@"(?i)Saldo actual:?\s*₡\s*(?<value>[\d][\d.,\s]*)")]
    private static partial Regex BalanceRegex();

    [GeneratedRegex(@"(?<date>\d{2}/\d{2}/\d{4})Pago(?<capital>₡[^₡]*)(?<interest>₡[^₡]*)(?<lateFee>₡[^₡]*)(?<other>₡[^₡]*)(?<total>₡[^₡]*)₡")]
    private static partial Regex PaymentRegex();

    [GeneratedRegex(@"(?<num>\d{1,2})\s+(?<date>\d{2}/\d{2}/\d{4})\s+(?<balance>₡[^₡]*)(?<capital>₡[^₡]*)(?<interest>₡[^₡]*)(?<lateFee>₡[^₡]*)(?<other>₡[^₡]*)(?<total>₡[^₡]*?)\s+(?<status>Pagada|Vigente)")]
    private static partial Regex CuotaRegex();
}
