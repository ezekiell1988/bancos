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

    private static string CreateSignature(string kind, string text)
    {
        var lines = text.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Take(24)
            .Select(line => Regex.Replace(ImportTemplateDetector.Normalize(line), "\\d+", "#"))
            .Select(line => Regex.Replace(line, "\\s+", " ").Trim());
        var structure = string.Join('\n', lines.Prepend(kind));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(structure)));
    }
}
