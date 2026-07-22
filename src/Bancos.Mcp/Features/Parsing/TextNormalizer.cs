using System.Globalization;
using System.Text;

namespace Bancos.Mcp.Features.Parsing;

internal static class TextNormalizer
{
    public static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormD)
        .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant();
}
