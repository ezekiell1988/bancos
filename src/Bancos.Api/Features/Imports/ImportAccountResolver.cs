using System.Text.RegularExpressions;
using Bancos.Api.Data;
using Bancos.Api.Domain;
using Bancos.Api.Features.Parsing;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Api.Features.Imports;

/// <summary>Resolves an auxiliary from identifiers contained in the uploaded content, never its file name or path.</summary>
public sealed partial class ImportAccountResolver(BancosDbContext db)
{
    public async Task<ImportPlan> ResolveAsync(string template, ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        var metadata = ImportReviewTemplates.Get(template);
        if (metadata is null) return new(null, "No pudimos identificar el tipo de archivo. Selecciona uno de los tipos disponibles.");
        if (!metadata.IsEnabled) return new(null, $"Identificamos {metadata.Label}, pero su extractor todavía no está disponible.");

        var candidates = await db.AccountAuxiliaries.AsNoTracking()
            .Where(x => x.Account!.Kind == metadata.AccountKind)
            .Select(x => new AuxiliaryCandidate(x.Id, x.Iban, x.Bank, x.CurrencyCode, x.CardNumberMasked))
            .ToListAsync(ct);
        if (candidates.Count == 0) return new(null, "No existe un auxiliar compatible para este tipo de archivo.");

        var text = ImportContentText.Extract(content).Text;
        var ibanMatches = IbanRegex().Matches(text).Select(x => NormalizeIban(x.Value)).Distinct(StringComparer.Ordinal).ToArray();
        var ibanCandidate = candidates.SingleOrDefault(candidate => candidate.Iban is not null && ibanMatches.Contains(NormalizeIban(candidate.Iban), StringComparer.Ordinal));
        if (ibanCandidate is not null) return new(ibanCandidate.Id, null);

        if (!IsCardTemplate(template))
            return candidates.Count == 1 ? new(candidates[0].Id, null) : new(null, "El archivo no contiene un IBAN identificable para resolver el auxiliar.");

        var lastFour = CardNumberRegex().Matches(text).Select(x => Digits(x.Value)).Where(x => x.Length >= 4).Select(x => x[^4..]).Distinct(StringComparer.Ordinal).ToArray();
        var cardCandidates = candidates.Where(candidate => candidate.CardNumberMasked is not null && lastFour.Contains(Digits(candidate.CardNumberMasked)[^4..], StringComparer.Ordinal)).ToArray();
        if (cardCandidates.Length == 0) return new(null, "El estado de tarjeta no contiene un identificador de tarjeta reconocido.");

        var currencies = ExtractCurrencies(content);
        if (currencies.Length != 1) return new(null, "El estado de tarjeta contiene más de una moneda; debe cargarse por estado monetario para asignarlo al auxiliar correcto.");
        var resolved = cardCandidates.SingleOrDefault(candidate => string.Equals(candidate.CurrencyCode, currencies[0], StringComparison.OrdinalIgnoreCase));
        return resolved is null
            ? new(null, "No existe un auxiliar de la moneda identificada para esta tarjeta.")
            : new(resolved.Id, null);
    }

    private static bool IsCardTemplate(string template) => template is ImportTemplates.BacCreditCsvV1 or ImportTemplates.BacCreditOnlinePdfV1 or ImportTemplates.BacAccountStatementPdfV1 or ImportTemplates.BnCardStatementPdfV1;

    private static string[] ExtractCurrencies(ReadOnlyMemory<byte> content)
    {
        try
        {
            return new CardStatementParser().Parse(content).Movements.Select(x => x.OriginalCurrencyCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (InvalidDataException)
        {
            return [];
        }
    }

    private static string NormalizeIban(string value) => string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();
    private static string Digits(string value) => string.Concat(value.Where(char.IsDigit));

    [GeneratedRegex(@"\bCR\s*\d(?:[\s-]*\d){19,31}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IbanRegex();

    [GeneratedRegex(@"(?<!\d)(?:\d{4}[\s-]?){2,3}\d{4}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex CardNumberRegex();

    private sealed record AuxiliaryCandidate(Guid Id, string? Iban, string? Bank, string? CurrencyCode, string? CardNumberMasked);
}
