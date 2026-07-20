using System.Globalization;
using System.Text.RegularExpressions;

namespace Bancos.Api.Features.Parsing;

public sealed record ParsedCoopealianzaLoanPayment(DateOnly PaymentDate, decimal Capital, decimal Interest, decimal LateFee, decimal OtherCharges, decimal Total);
public sealed record ParsedCoopealianzaLoan(decimal OutstandingBalance, IReadOnlyList<ParsedCoopealianzaLoanPayment> Payments);

/// <summary>Parses the documented Coopealianza loan PDF text, including its payment composition.</summary>
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
        var normalized = ImportTemplateDetector.Normalize(text);
        if (!ContainsRequiredSignature(normalized)) throw new InvalidDataException("PDF does not contain the documented Coopealianza loan signature.");

        var balanceMatch = BalanceRegex().Match(text);
        if (!balanceMatch.Success) throw new InvalidDataException("Coopealianza loan PDF does not contain a valid outstanding balance.");

        var payments = PaymentRegex().Matches(text)
            .Select(match => new ParsedCoopealianzaLoanPayment(
                ParseDate(match.Groups["date"].Value),
                ParseAmount(match.Groups["capital"].Value, "capital"),
                ParseAmount(match.Groups["interest"].Value, "interest"),
                ParseAmount(match.Groups["lateFee"].Value, "mora"),
                ParseAmount(match.Groups["other"].Value, "otros"),
                ParseAmount(match.Groups["total"].Value, "total")))
            .ToArray();

        // Some valid statements only report the current balance. Persisting that snapshot is
        // useful and does not invent a payment that was not present in the source document.
        foreach (var payment in payments)
        {
            var components = payment.Capital + payment.Interest + payment.LateFee + payment.OtherCharges;
            if (Math.Abs(components - payment.Total) > ReconciliationTolerance)
                throw new InvalidDataException($"Coopealianza payment on {payment.PaymentDate:yyyy-MM-dd} does not reconcile to its total.");
        }

        return new ParsedCoopealianzaLoan(ParseAmount(balanceMatch.Groups["balance"].Value, "saldo"), payments);
    }

    private static bool ContainsRequiredSignature(string text) => new[] { "ver detalles del prestamo", "capital", "interes", "mora", "otros", "total", "saldo" }.All(text.Contains);
    private static DateOnly ParseDate(string value) => DateOnly.TryParseExact(value, ["dd/MM/yyyy", "d/M/yyyy"], CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.None, out var result)
        ? result : throw new InvalidDataException($"Invalid Coopealianza payment date '{value}'.");
    private static decimal ParseAmount(string value, string field)
    {
        if (MoneyParser.TryParse(value, out var result)) return result;
        throw new InvalidDataException($"Invalid Coopealianza loan {field} '{value}'.");
    }

    // Matches "Saldo actual:₡ 4 372 249,85" — Bankingly PDFs use regular or non-breaking spaces as thousands separators.
    [GeneratedRegex(@"(?i)Saldo actual:₡\s*(?<balance>[\d][\d.,\s]*)")]
    private static partial Regex BalanceRegex();

    // Matches payment history rows: "TxNum dd/MM/yyyy Pago ₡Capital ₡Interés ₡Mora ₡Otros ₡Total ₡SaldoRestante"
    // ₡ acts as a natural delimiter; [^₡]* captures the number including space-thousands separators.
    [GeneratedRegex(@"(?<date>\d{2}/\d{2}/\d{4})Pago(?<capital>₡[^₡]*)(?<interest>₡[^₡]*)(?<lateFee>₡[^₡]*)(?<other>₡[^₡]*)(?<total>₡[^₡]*)₡")]
    private static partial Regex PaymentRegex();
}
