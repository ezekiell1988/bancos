using System.Text.RegularExpressions;

namespace Bancos.Mcp.Features.Classification;

internal static partial class AiDescriptionSanitizer
{
    public static string Sanitize(string normalizedDescription)
    {
        var sanitized = EmailRegex().Replace(normalizedDescription, "[correo]");
        sanitized = IbanRegex().Replace(sanitized, "[iban]");
        sanitized = AmountRegex().Replace(sanitized, "[monto]");
        sanitized = SeparatedNumberRegex().Replace(sanitized, "[identificador]");
        sanitized = LongNumberRegex().Replace(sanitized, "[identificador]");
        return WhitespaceRegex().Replace(sanitized, " ").Trim();
    }

    [GeneratedRegex(@"\b[\w.+-]+@[\w.-]+\.[a-z]{2,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\bcr\d{20,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex IbanRegex();

    [GeneratedRegex(@"(?:crc|usd|₡|\$)\s*\d[\d.,\s]*", RegexOptions.CultureInvariant)]
    private static partial Regex AmountRegex();

    [GeneratedRegex(@"(?<!\d)(?:\d[ -]?){12,19}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex SeparatedNumberRegex();

    [GeneratedRegex(@"(?<!\d)\d{6,}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex LongNumberRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
