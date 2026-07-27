using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bancos.Mcp.Features.Parsing;

public sealed record BnCardStatementIdentity(string IdentifierHash, string CardFingerprint);

public sealed partial class BnCardStatementPdfParser
{
    internal const decimal BalanceTolerance = 0.01m;

    public static BnCardStatementIdentity ExtractIdentityFingerprints(ReadOnlyMemory<byte> content)
    {
        var extracted = ImportContentText.Extract(content);
        if (extracted.Kind != "pdf") throw new InvalidDataException("El estado de tarjeta BN debe ser un PDF.");

        return ExtractIdentityFingerprintsFromText(extracted.Text);
    }

    internal static BnCardStatementIdentity ExtractIdentityFingerprintsFromText(string text)
    {
        var identifiers = IbanRegex().Matches(text)
            .Select(match => NormalizeIban(match.Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (identifiers.Length != 1 || identifiers.Any(identifier => !HasValidIbanChecksum(identifier)))
            throw new InvalidDataException(
                "El estado BN debe contener una única identidad bancaria válida.");

        var cards = CardNumberRegex().Matches(text)
            .Select(match => NormalizeMaskedCard(match.Groups["value"].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (cards.Length != 1 || cards.Any(card => !IsSupportedCardIdentity(card)))
            throw new InvalidDataException(
                "El estado BN debe contener una única identidad de tarjeta válida.");

        return new BnCardStatementIdentity(Hash(identifiers[0]), Hash(cards[0]));
    }

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
        var (previousBalanceCrc, previousBalanceUsd) = ParseCurrencyPair(
            PreviousBalanceRegex(), text, "saldo anterior");
        var (currentBalanceCrc, currentBalanceUsd) = ParseCurrencyPair(
            CurrentBalanceRegex(), text, "saldo al corte");

        var minMatch = TotalMinimumRegex().Match(text);
        var cashMatch = TotalCashRegex().Match(text);
        if (!minMatch.Success || !cashMatch.Success)
            throw new InvalidDataException("No se encontraron totales de pago mínimo y de contado en el estado BN.");

        var minCrc = ParseAmount(minMatch.Groups["crc"].Value, "pago mínimo colones");
        var minUsd = ParseAmount(minMatch.Groups["usd"].Value, "pago mínimo dólares");
        var cashCrc = ParseAmount(cashMatch.Groups["crc"].Value, "pago de contado colones");
        var cashUsd = ParseAmount(cashMatch.Groups["usd"].Value, "pago de contado dólares");

        var movements = ParseMovements(text);
        ValidateBalance("CRC", previousBalanceCrc, currentBalanceCrc, movements);
        ValidateBalance("USD", previousBalanceUsd, currentBalanceUsd, movements);
        var financingLines = ParseFinancingLines(text);

        return new ParsedBnCardStatement(
            cardNumber.Trim(), brand.Trim(), loyalty.Trim(),
            statementDate, paymentDue,
            previousBalanceCrc, previousBalanceUsd,
            currentBalanceCrc, currentBalanceUsd,
            minCrc, minUsd, cashCrc, cashUsd,
            movements, financingLines);
    }

    private static IReadOnlyList<ParsedCardMovement> ParseMovements(string text)
    {
        var movements = new List<ParsedCardMovement>();
        var paymentSection = ExtractSection(text, PaymentSectionStartRegex(), PaymentSectionEndRegex());
        if (paymentSection is not null)
        {
            foreach (Match m in PaymentRowRegex().Matches(paymentSection))
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

        var purchaseSection = ExtractSection(text, PurchaseSectionStartRegex(), PurchaseSectionEndRegex(), allowOpenEnd: true);
        if (purchaseSection is not null)
        {
            AddDetailRows(purchaseSection, CardOperationKind.Purchase, "compra", movements);
        }

        return movements;
    }

    private static void AddDetailRows(
        string section,
        CardOperationKind operation,
        string referencePrefix,
        List<ParsedCardMovement> movements)
    {
        foreach (Match match in PurchaseRowRegex().Matches(section))
        {
            if (!TryParseDate(match.Groups["date"].Value, out var date)) continue;
            MoneyParser.TryParse(match.Groups["crc"].Value, out var crcAmount);
            MoneyParser.TryParse(match.Groups["usd"].Value, out var usdAmount);
            if (crcAmount == 0 && usdAmount == 0) continue;

            var description = match.Groups["description"].Value.Trim();
            if (string.IsNullOrWhiteSpace(description)) continue;

            if (crcAmount != 0)
                movements.Add(new ParsedCardMovement(
                    date, $"bn-{referencePrefix}-{movements.Count + 1}", description,
                    crcAmount, "CRC", crcAmount, operation));
            if (usdAmount != 0)
                movements.Add(new ParsedCardMovement(
                    date, $"bn-{referencePrefix}-{movements.Count + 1}", description,
                    usdAmount, "USD", null, operation));
        }
    }

    private static string? ExtractSection(string text, Regex startRegex, Regex endRegex, bool allowOpenEnd = false)
    {
        var start = startRegex.Match(text);
        if (!start.Success) return null;

        var end = endRegex.Match(text, start.Index + start.Length);
        if (!end.Success && !allowOpenEnd) return null;

        var endIndex = end.Success ? end.Index : text.Length;
        return text[(start.Index + start.Length)..endIndex];
    }

    private static void ValidateBalance(
        string currencyCode,
        decimal previousBalance,
        decimal currentBalance,
        IReadOnlyList<ParsedCardMovement> movements)
    {
        var netMovements = movements
            .Where(movement => movement.OriginalCurrencyCode == currencyCode)
            .Sum(movement => movement.Operation == CardOperationKind.Payment
                ? -Math.Abs(movement.OriginalAmount)
                : Math.Abs(movement.OriginalAmount));
        if (Math.Abs(previousBalance + netMovements - currentBalance) > BalanceTolerance)
            throw new InvalidDataException(
                $"El estado BN no concilia en {currencyCode} con tolerancia {BalanceTolerance:0.00}.");
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
        throw new InvalidDataException($"Fecha inválida en campo '{field}'.");
    }

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParse(value, CultureInfo.GetCultureInfo("es-CR"), DateTimeStyles.AllowWhiteSpaces, out date)
        || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date);

    private static decimal ParseAmount(string value, string field)
    {
        if (MoneyParser.TryParse(value, out var result)) return result;
        throw new InvalidDataException($"Monto inválido en campo '{field}'.");
    }

    private static (decimal Crc, decimal Usd) ParseCurrencyPair(Regex lineRegex, string text, string field)
    {
        var line = lineRegex.Matches(text)
            .FirstOrDefault(match => MoneyAmountRegex().Matches(match.Value).Count >= 4);
        if (line is null)
            throw new InvalidDataException($"Campo '{field}' no encontrado en estado BN.");

        var amounts = MoneyAmountRegex().Matches(line.Value);
        return (
            ParseAmount(amounts[0].Value, $"{field} CRC"),
            ParseAmount(amounts[2].Value, $"{field} USD"));
    }

    private static string NormalizeCurrency(string value) =>
        value.Contains("DOLAR", StringComparison.OrdinalIgnoreCase) || value.Contains("USD", StringComparison.OrdinalIgnoreCase) ? "USD" : "CRC";

    private static string NormalizeIban(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    private static string NormalizeMaskedCard(string value) =>
        string.Concat(value
            .ToUpperInvariant()
            .Where(character => char.IsDigit(character) || character is '*' or 'X'))
            .Replace('X', '*');

    private static bool IsSupportedCardIdentity(string value) =>
        value.Length is >= 8 and <= 19
        && value[^4..].All(char.IsDigit)
        && value[..^4].All(character => char.IsDigit(character) || character == '*');

    private static bool HasValidIbanChecksum(string value)
    {
        if (value.Length != 22 || !value.StartsWith("CR", StringComparison.Ordinal))
            return false;

        var rearranged = string.Concat(value.AsSpan(4), value.AsSpan(0, 4));
        var remainder = 0;
        foreach (var character in rearranged)
        {
            if (char.IsDigit(character))
            {
                remainder = ((remainder * 10) + (character - '0')) % 97;
                continue;
            }

            if (character is < 'A' or > 'Z') return false;
            remainder = ((remainder * 100) + character - 'A' + 10) % 97;
        }

        return remainder == 1;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    [GeneratedRegex(@"N[uú]mero de cuenta\s*:?\s*(?<value>[*\d][*\d\s-]{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex CardNumberRegex();

    [GeneratedRegex(@"Marca de la tarjeta\s*:?\s*(?<value>.+?)(?=N[uú]mero de cuenta|C[eé]dula|\r?\n)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex BrandRegex();

    [GeneratedRegex(@"Plan de Lealtad\s*:?\s*(?<value>.+?)(?=Cuenta IBAN|\r?\n)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex LoyaltyPlanRegex();

    [GeneratedRegex(@"Fecha de emisi[oó]n y corte\s*:?\s*(?<value>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex StatementDateRegex();

    [GeneratedRegex(@"Fecha l[ií]mite de pago de contado\s*:?\s*(?<value>\d{2}/\d{2}/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex PaymentDueDateRegex();

    [GeneratedRegex(@"TOTAL PAGO M[IÍ]NIMO\*?\s*(?<crc>[\d,]+\.\d{2})\s+(?<usd>[\d,]+\.\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex TotalMinimumRegex();

    [GeneratedRegex(@"TOTAL PAGO DE CONTADO\*?\s*(?<crc>[\d,]+\.\d{2})\s+(?<usd>[\d,]+\.\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex TotalCashRegex();

    [GeneratedRegex(@"^.*Saldo anterior.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex PreviousBalanceRegex();

    [GeneratedRegex(@"^.*Saldo al corte.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex CurrentBalanceRegex();

    [GeneratedRegex(@"^\s*(?<date>\d{2}/\d{2}/\d{4})\s*DB CTA\s*(?<ref>\d+)\s+(?<crc>-?[\d,]+\.\d{2})\s+[\d,]+\.\d{2}\s+(?<usd>-?[\d,]+\.\d{2})\s+[\d,]+\.\d{2}\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex PaymentRowRegex();

    [GeneratedRegex(@"^\s*(?<date>\d{2}/\d{2}/\d{4})\s+(?<description>.+?)\s+(?<crc>-?[\d,]+\.\d{2})(?:\s+-?[\d,]+\.\d{2})?\s+(?<usd>-?[\d,]+\.\d{2})(?:\s+-?[\d,]+\.\d{2})?\s*$", RegexOptions.Multiline)]
    private static partial Regex PurchaseRowRegex();

    [GeneratedRegex(@"Detalle de pagos y cr[eé]ditos del per[ií]odo", RegexOptions.IgnoreCase)]
    private static partial Regex PaymentSectionStartRegex();

    [GeneratedRegex(@"Total pagos recibidos", RegexOptions.IgnoreCase)]
    private static partial Regex PaymentSectionEndRegex();

    [GeneratedRegex(@"Detalle de compras del per[ií]odo", RegexOptions.IgnoreCase)]
    private static partial Regex PurchaseSectionStartRegex();

    [GeneratedRegex(@"Total de compras del per[ií]odo", RegexOptions.IgnoreCase)]
    private static partial Regex PurchaseSectionEndRegex();

    [GeneratedRegex(@"-?[\d,]+\.\d{2}")]
    private static partial Regex MoneyAmountRegex();

    [GeneratedRegex(@"(?<![A-Z0-9])CR(?:[ \t-]*\d){20}(?!\d)", RegexOptions.IgnoreCase)]
    private static partial Regex IbanRegex();

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
