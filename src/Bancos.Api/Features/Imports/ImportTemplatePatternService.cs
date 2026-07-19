using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Features.Parsing;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Api.Features.Imports;

/// <summary>Matches locally confirmed document structures without persisting document text or bytes.</summary>
public sealed class ImportTemplatePatternService(BancosDbContext db)
{
    public async Task<ImportTemplateDetection> DetectAsync(ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        var extracted = ImportContentText.Extract(content);
        var hash = CreateSignature(extracted.Kind, extracted.Text);
        var pattern = await db.ImportTemplatePatterns.AsNoTracking().SingleOrDefaultAsync(x => x.SignatureHash == hash, ct);
        return pattern is null
            ? ImportTemplateDetector.Detect(extracted.Text, extracted.Kind)
            : new ImportTemplateDetection(pattern.Template, extracted.Kind, ["patrón confirmado localmente"]);
    }

    public async Task LearnAsync(ReadOnlyMemory<byte> content, string template, CancellationToken ct)
    {
        var extracted = ImportContentText.Extract(content);
        var hash = CreateSignature(extracted.Kind, extracted.Text);
        if (await db.ImportTemplatePatterns.AnyAsync(x => x.SignatureHash == hash, ct)) return;
        db.ImportTemplatePatterns.Add(new ImportTemplatePattern { SignatureHash = hash, ContentKind = extracted.Kind, Template = template });
        await db.SaveChangesAsync(ct);
    }

    private static string CreateSignature(string kind, string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CreateStructure(kind, text))));

    /// <summary>
    /// Produces a document-format fingerprint without retaining document words, values,
    /// identifiers, or filenames. It intentionally describes only layout and cell classes.
    /// </summary>
    private static string CreateStructure(string kind, string text)
    {
        var lines = text.Replace("\r", string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(24)
            .Select(DescribeLine);
        return string.Join('\n', lines.Prepend($"kind:{kind}"));
    }

    private static string DescribeLine(string line)
    {
        var delimiter = Delimiters.FirstOrDefault(line.Contains);
        if (delimiter == default) return $"line:{DescribeText(line)}";

        var cells = line.Split(delimiter);
        return $"delimited:{delimiter}:{cells.Length}:{string.Join(',', cells.Select(DescribeText))}";
    }

    private static string DescribeText(string value)
    {
        var compact = Regex.Replace(value, "\\s+", " ").Trim();
        if (compact.Length == 0) return "empty";
        if (DatePattern.IsMatch(compact)) return "date";
        if (NumberPattern.IsMatch(compact)) return "number";

        var hasLetters = compact.Any(char.IsLetter);
        var hasDigits = compact.Any(char.IsDigit);
        return (hasLetters, hasDigits) switch
        {
            (true, false) => "text",
            (false, true) => "mixed-symbols",
            (true, true) => "mixed",
            _ => "symbols"
        };
    }

    private static readonly char[] Delimiters = [';', ',', '\t', '|'];
    private static readonly Regex DatePattern = new("^\\d{1,4}[-/.]\\d{1,2}[-/.]\\d{1,4}$", RegexOptions.CultureInvariant);
    private static readonly Regex NumberPattern = new("^[+-]?[₡$€]?\\s*\\d{1,3}(?:[., ]\\d{3})*(?:[.,]\\d+)?%?$", RegexOptions.CultureInvariant);
}
