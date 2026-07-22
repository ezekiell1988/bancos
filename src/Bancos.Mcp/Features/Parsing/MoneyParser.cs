using System.Globalization;
using System.Text.RegularExpressions;

namespace Bancos.Mcp.Features.Parsing;

internal static partial class MoneyParser
{
    public static bool TryParse(string? value, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(value)) return true;

        var normalized = CurrencyMarker().Replace(value, string.Empty).Trim();
        normalized = Regex.Replace(normalized, @"\s+", string.Empty);
        var lastDot = normalized.LastIndexOf('.');
        var lastComma = normalized.LastIndexOf(',');
        if (lastDot >= 0 && lastComma >= 0)
        {
            var decimalSeparator = Math.Max(lastDot, lastComma);
            var canonical = normalized.Replace(decimalSeparator == lastDot ? "," : ".", string.Empty)
                .Replace(decimalSeparator == lastDot ? "." : ",", ".");
            return decimal.TryParse(canonical, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount);
        }

        if (lastDot >= 0)
        {
            var decimalDigits = normalized.Length - lastDot - 1;
            var culture = decimalDigits == 3 ? CultureInfo.GetCultureInfo("es-CR") : CultureInfo.InvariantCulture;
            return decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowLeadingSign, culture, out amount);
        }

        if (lastComma >= 0)
            return decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("es-CR"), out amount);

        return decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount);
    }

    [GeneratedRegex(@"(?i)(?:US\$|USD|CRC|₡|\$)")]
    private static partial Regex CurrencyMarker();
}
