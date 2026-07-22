using System.Globalization;
using System.Text.RegularExpressions;

namespace Bancos.Mcp.Features.Parsing;

public sealed partial class BnCardStatementPdfParser
{
    public ParsedBnCardStatement Parse(ReadOnlyMemory<byte> content)
    {
        var extracted = ImportContentText.Extract(content);
        if (extracted.Kind != "pdf") throw new InvalidDataException("El estado de tarjeta BN debe ser un PDF.");
        return ParseText(extracted.Text);
    }

    internal static ParsedBnCardStatement ParseText(string text)
    {
        var normalized = TextNormalizer.Normalize(text);
        if (!normalized.Contains("banco nacional de costa rica") || !normalized.Contains("detalle de compras del periodo"))
            throw new InvalidDataException("El PDF no contiene la firma del estado de tarjeta Banco Nacional.");

        var cardNumber = RequireMatch(CardNumberRegex(), text, "número de cuenta");
        var brand = RequireMatch(BrandRegex(), text, "marca de la tarjeta");
        var loyalty = RequireMatch(LoyaltyPlanRegex(), text, "plan de lealtad");
        var statementDate = ParseDate(RequireMatch(StatementDateRegex(), text, "fecha de emisión y corte"), "fecha de corte");
        var paymentDue = ParseDate(RequireMatch(PaymentDueDateRegex(), text, "fecha límite de pago de contado"), "fecha límite de pago");

        var minMatch = TotalMinimumRegex().Match(text);
        var cashMatch = TotalCashRegex().Match(text);
        if (!minMatch.Success || !cashMatch.Success)
            throw new InvalidDataException("No se encontraron totales de pago mínimo y de contado en el estado BN.");

        var minCrc = ParseAmount(minMatch.Groups["crc"].Value, "pago mínimo colones");
        var minUsd = ParseAmount(minMatch.Groups["usd"].Value, "pago mínimo dólares");
        var cashCrc = ParseAmount(cashMatch.Groups["crc"].Value, "pago de contado colones");
        var cashUsd = ParseAmount(cashMatch.Groups["usd"].Value, "pago de contado dólares");

        var movements = ParseMovements(text);
        var financingLines = ParseFinancingLines(text);

        return new ParsedBnCardStatement(
            cardNumber.Trim(), brand.Trim(), loyalty.Trim(),
            statementDate, paymentDue,
            minCrc, minUsd, cashCrc, cashUsd,
            movements, financingLines);
    }

    private static IReadOnlyList<ParsedCardMovement> ParseMovements(string text)
    {
        var movements = new List<ParsedCardMovement>();
        var normalized = TextNormalizer.Normalize(text);

        var paymentStart = normalized.IndexOf("detalle de pagos y creditos del periodo", StringComparison.Ordinal);
        var paymentEnd = normalized.IndexOf("total pagos recibidos", StringComparison.Ordinal);
        var purchaseStart = normalized.IndexOf("detalle de compras del periodo", StringComparison.Ordinal);
        var purchaseEnd = normalized.IndexOf("total de compras del periodo", StringComparison.Ordinal);

        if (paymentStart >= 0 && paymentEnd > paymentStart)
        {
            var section = text[paymentStart..paymentEnd];
            foreach (Match m in PaymentRowRegex().Matches(section))
            {
                if (!TryParseDate(m.Groups["date"].Value, out var date)) continue;
                MoneyParser.TryParse(m.Groups["crc"].Value, out var crcAmt);
                MoneyParser.TryParse(m.Groups["usd"].Value, out var usdAmt);
                var desc = "DB CTA " + m.Groups["ref"].Value.Trim();
                if (crcAmt != 0)
                    movements.Add(new ParsedCardMovement(date, $"bn-pago-{movements.Count + 1}", desc, Math.Abs(crcAmt), "CRC", Math.Abs(crcAmt), CardOperationKind.Payment));
                else if (usdAmt != 0)
                    movements.Add(new ParsedCardMovement(date, $"bn-pago-{movements.Count + 1}", desc, Math.Abs(usdAmt), "USD", null, CardOperationKind.Payment));
            }
        }

        if (purchaseStart >= 0)
        {
            var end = purchaseEnd > purchaseStart ? purchaseEnd : text.Length;
            var section = text[purchaseStart..end];
            var purchaseMatches = PurchaseRowRegex().Matches(section);
            foreach (Match m in purchaseMatches)
            {
                if (!TryParseDate(m.Groups["date"].Value, out var date)) continue;
                MoneyParser.TryParse(m.Groups["crc"].Value, out var crcAmt);
                MoneyParser.TryParse(m.Groups["usd"].Value, out var usdAmt);
                if (crcAmt == 0 && usdAmt == 0) continue;
                var description = m.Groups["description"].Value.Trim();
                if (string.IsNullOrWhiteSpace(description)) continue;
                if (crcAmt != 0)
                    movements.Add(new ParsedCardMovement(date, $"bn-compra-{movements.Count + 1}", description, crcAmt, "CRC", crcAmt, CardOperationKind.Purchase));
                else
                    movements.Add(new ParsedCardMovement(date, $"bn-compra-{movements.Count + 1}", description, usdAmt, "USD", null, CardOperationKind.Purchase));
            }
        }

        return movements;
    }

    private static IReadOnlyList<ParsedBnFinancingLine> ParseFinancingLines(string text)
    {
        var results = new List<ParsedBnFinancingLine>();
        var blocks = FinancingBlockSplitRegex().Split(text).Skip(1);
        foreach (var block in blocks)
        {
            var pendingMatch = PendingBalanceRegex().Match(block);
            if (!pendingMatch.Success) continue;
            if (!MoneyParser.TryParse(pendingMatch.Groups["amount"].Value, out var outstanding) || outstanding == 0) continue;

            var originMatch = OriginRegex().Match(block);
            var amountMatch = LoanAmountRegex().Match(block);
            var installmentAmtMatch = InstallmentAmountRegex().Match(block);
            var currencyMatch = CurrencyRegex().Match(block);
            var startDateMatch = StartDateRegex().Match(block);
            var endDateMatch = EndDateRegex().Match(block);
            var totalInstallmentsMatch = TotalInstallmentsRegex().Match(block);
            var currentInstallmentMatch = CurrentInstallmentRegex().Match(block);

            if (!originMatch.Success || !amountMatch.Success) continue;

            var origin = originMatch.Groups["value"].Value.Trim();
            var currency = currencyMatch.Success ? NormalizeCurrency(currencyMatch.Groups["value"].Value) : "CRC";
            MoneyParser.TryParse(amountMatch.Groups["value"].Value, out var originalAmount);
            MoneyParser.TryParse(installmentAmtMatch.Success ? installmentAmtMatch.Groups["value"].Value : "0", out var installmentAmt);

            int totalInstallments = 0, currentInstallment = 0;
            if (totalInstallmentsMatch.Success) int.TryParse(totalInstallmentsMatch.Groups["total"].Value, out totalInstallments);
            if (currentInstallmentMatch.Success) int.TryParse(currentInstallmentMatch.Groups["current"].Value, out currentInstallment);

            DateOnly startDate = default, endDate = default;
            if (startDateMatch.Success) TryParseDate(startDateMatch.Groups["value"].Value, out startDate);
            if (endDateMatch.Success) TryParseDate(endDateMatch.Groups["value"].Value, out endDate);

            results.Add(new ParsedBnFinancingLine(origin, currency, originalAmount, outstanding, installmentAmt, totalInstallments, currentInstallment, startDate, endDate));
        }
        return results;
    }

    private static string RequireMatch(Regex regex, string text, string field)
    {
        var m = regex.Match(text);
        return m.Success ? m.Groups["value"].Value.Trim() : throw new InvalidDataException($"Campo '{field}' no encontrado en estado BN.");
    }

    private static DateOnly ParseDate(string value, string field)
    {
        if (TryParseDate(value.Trim(), out var date)) return date;
        throw new InvalidDataException($"Fecha inválida '{value}' en campo '{field}'.");
    }

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParse(value, CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.AllowWhiteSpaces, out date)
        || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date);

    private static decimal ParseAmount(string value, string field)
    {
        if (MoneyParser.TryParse(value, out var result)) return result;
        throw new InvalidDataException($"Monto inválido '{value}' en campo '{field}'.");
    }

    private static string NormalizeCurrency(string value) =>
        value.Contains("DOLAR", StringComparison.OrdinalIgnoreCase) || value.Contains("USD", StringComparison.OrdinalIgnoreCase) ? "USD" : "CRC";

    [GeneratedRegex(@"N[uú]mero de cuenta(?<value>[*\d]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CardNumberRegex();

    [GeneratedRegex(@"Marca de la tarjeta(?<value>.+?)(?=N[uú]mero de cuenta|C[eé]dula)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex BrandRegex();

    [GeneratedRegex(@"Plan de Lealtad(?<value>.+?)(?=Cuenta IBAN)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex LoyaltyPlanRegex();

    [GeneratedRegex(@"Fecha de emisi[oó]n y corte(?<value>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex StatementDateRegex();

    [GeneratedRegex(@"Fecha l[ií]mite de pago de contado(?<value>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex PaymentDueDateRegex();

    [GeneratedRegex(@"TOTAL PAGO M[IÍ]NIMO\*?(?<crc>[\d,]+\.\d{2})(?<usd>[\d,]+\.\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex TotalMinimumRegex();

    [GeneratedRegex(@"TOTAL PAGO DE CONTADO\*?(?<crc>[\d,]+\.\d{2})(?<usd>[\d,]+\.\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex TotalCashRegex();

    [GeneratedRegex(@"(?<date>\d{2}/\d{2}/\d{4})DB CTA (?<ref>\d+)(?<crc>-?[\d,]+\.\d{2})(?:[\d,]+\.\d{2})(?<usd>-?[\d,]+\.\d{2})(?:[\d,]+\.\d{2})")]
    private static partial Regex PaymentRowRegex();

    [GeneratedRegex(@"(?<date>\d{2}/\d{2}/\d{4})(?<description>.+?)(?<crc>[\d,]+\.\d{2})(?<usd>[\d,]+\.\d{2})(?=\d{2}/\d{2}/\d{4}|TOTAL|\z)", RegexOptions.Singleline)]
    private static partial Regex PurchaseRowRegex();

    [GeneratedRegex(@"OTRAS L[IÍ]NEAS DE FINANCIAMIENTO Y OTROS", RegexOptions.IgnoreCase)]
    private static partial Regex FinancingBlockSplitRegex();

    [GeneratedRegex(@"Origen del cr[eé]dito:(?<value>.+?)(?=Tasa de Inter)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex OriginRegex();

    [GeneratedRegex(@"Monto del cr[eé]dito:(?<value>[\d,]+\.\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex LoanAmountRegex();

    [GeneratedRegex(@"Monto de la cuota otra l[ií]nea de financiamiento\(\*+\):(?<value>[\d,]+\.\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex InstallmentAmountRegex();

    [GeneratedRegex(@"Moneda del cr[eé]dito:(?<value>\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex CurrencyRegex();

    [GeneratedRegex(@"Fecha de inicio del cr[eé]dito:(?<value>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex StartDateRegex();

    [GeneratedRegex(@"Fecha de finalizaci[oó]n:(?<value>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex EndDateRegex();

    [GeneratedRegex(@"Saldo pendiente:(?<amount>[\d,]+\.\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex PendingBalanceRegex();

    [GeneratedRegex(@"Plazo meses:(?<total>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex TotalInstallmentsRegex();

    [GeneratedRegex(@"PAGO DE LA CUOTA (?<current>\d+) DE ", RegexOptions.IgnoreCase)]
    private static partial Regex CurrentInstallmentRegex();
}
