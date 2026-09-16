using Bancos.Mcp.Data;
using Bancos.Mcp.Features.Parsing;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bancos.Mcp.Features.FileProcessing;

public sealed record FinancingAccountPair(Guid CrcAccountId, Guid UsdAccountId);
public sealed record CardStatementAccountPair(Guid CrcAccountId, Guid? UsdAccountId);
internal sealed record BnCardAccountCandidate(
    Guid AccountId,
    string CurrencyCode,
    string? IdentifierHash,
    string? CardFingerprint);

public sealed class AccountResolver(McpCatalogDbContext db)
{
    private static readonly Regex IbanPattern = new(@"CR\d{20}", RegexOptions.Compiled);
    private static readonly string[] DebitCsvParserKeys = ["bcr-debit-csv", "bn-debit-csv", "bn-debit-csv-crc"];

    public async Task<Guid> ResolveAsync(
        Guid templateId,
        Guid? bankAccountId,
        ReadOnlyMemory<byte> fileContent,
        CancellationToken ct)
    {
        if (bankAccountId.HasValue)
        {
            var exists = await db.BankAccountImportTemplates
                .AnyAsync(x => x.BankAccountId == bankAccountId.Value && x.ImportTemplateId == templateId, ct);
            if (!exists) throw new InvalidOperationException("La cuenta especificada no está vinculada a esta plantilla.");
            return bankAccountId.Value;
        }

        var accounts = await db.BankAccountImportTemplates
            .Where(x => x.ImportTemplateId == templateId)
            .Select(x => new { x.BankAccountId, x.BankAccount!.IdentifierHash, x.BankAccount.CardFingerprint })
            .ToListAsync(ct);

        if (accounts.Count == 1) return accounts[0].BankAccountId;
        if (accounts.Count == 0) throw new InvalidOperationException("No hay cuentas bancarias vinculadas a esta plantilla.");

        var fingerprints = BacCreditFinancingXlsParser.ExtractIdentifierFingerprints(fileContent);
        var matches = accounts
            .Where(account =>
                (!string.IsNullOrWhiteSpace(account.IdentifierHash) && fingerprints.Contains(account.IdentifierHash)) ||
                (!string.IsNullOrWhiteSpace(account.CardFingerprint) && fingerprints.Contains(account.CardFingerprint)))
            .Select(account => account.BankAccountId)
            .Distinct()
            .ToArray();

        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException("No se pudo identificar una cuenta única desde el contenido del archivo."),
            _ => throw new InvalidOperationException("El contenido del archivo coincide con más de una cuenta bancaria.")
        };
    }

    public async Task<CardStatementAccountPair> ResolveBnCardStatementPairAsync(
        Guid templateId,
        ReadOnlyMemory<byte> fileContent,
        CancellationToken ct)
    {
        var accounts = await db.BankAccountImportTemplates
            .Where(x => x.ImportTemplateId == templateId)
            .Select(x => new BnCardAccountCandidate(
                x.BankAccountId,
                x.BankAccount!.CurrencyCode,
                x.BankAccount.IdentifierHash,
                x.BankAccount.CardFingerprint))
            .ToListAsync(ct);

        if (accounts.Count == 0)
            throw new InvalidOperationException("No hay cuentas bancarias vinculadas a esta plantilla.");

        var identity = BnCardStatementPdfParser.ExtractIdentityFingerprints(fileContent);
        return ResolveBnCardStatementPair(accounts, identity);
    }

    internal static CardStatementAccountPair ResolveBnCardStatementPair(
        IReadOnlyList<BnCardAccountCandidate> accounts,
        BnCardStatementIdentity identity)
    {
        var matches = accounts
            .Where(account =>
                string.Equals(account.IdentifierHash, identity.IdentifierHash, StringComparison.OrdinalIgnoreCase)
                && string.Equals(account.CardFingerprint, identity.CardFingerprint, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var crcMatches = matches
            .Where(account => account.CurrencyCode == "CRC")
            .Select(account => account.AccountId)
            .Distinct()
            .ToArray();
        var usdMatches = matches
            .Where(account => account.CurrencyCode == "USD")
            .Select(account => account.AccountId)
            .Distinct()
            .ToArray();

        if (crcMatches.Length != 1 || usdMatches.Length != 1)
            throw new InvalidOperationException(
                "La identidad bancaria y de tarjeta del estado BN no coincide con un par CRC/USD registrado.");

        return new CardStatementAccountPair(crcMatches[0], usdMatches[0]);
    }

    public async Task<Guid> ResolveCrcByPathAsync(string relativePath, CancellationToken ct)
    {
        var ibanHashes = ExtractIbanHashes(relativePath);

        if (ibanHashes.Count == 0)
            throw new InvalidOperationException("No se encontraron IBANs en el nombre de carpeta del archivo.");

        var account = await db.BankAccounts
            .Where(a => a.IdentifierHash != null && ibanHashes.Contains(a.IdentifierHash) && a.CurrencyCode == "CRC")
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);

        return account ?? throw new InvalidOperationException("No se encontró cuenta CRC para los IBANs del archivo. Verifique que los IBANs estén registrados en el catálogo.");
    }

    public async Task<Guid> ResolveLinkedAccountByIbanPathAsync(
        string relativePath, Guid templateId, CancellationToken ct)
    {
        var ibanHashes = ExtractIbanHashes(relativePath);
        if (ibanHashes.Count == 0)
            throw new InvalidOperationException("No se encontraron IBANs en el nombre de carpeta del archivo.");

        var matches = await db.BankAccountImportTemplates
            .Where(x => x.ImportTemplateId == templateId
                && x.BankAccount!.IdentifierHash != null
                && ibanHashes.Contains(x.BankAccount.IdentifierHash!))
            .Select(x => x.BankAccountId)
            .Distinct()
            .ToArrayAsync(ct);

        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException("No se encontró una cuenta vinculada a la plantilla para los IBANs del archivo."),
            _ => throw new InvalidOperationException("Los IBANs del archivo coinciden con más de una cuenta vinculada a la plantilla.")
        };
    }

    public async Task<(Guid AccountId, Guid TemplateId)?> TryResolveDebitCsvByIbanPathAsync(
        string relativePath, CancellationToken ct)
    {
        var ibanHashes = ExtractIbanHashes(relativePath);
        if (ibanHashes.Count == 0) return null;

        var matches = await db.BankAccountImportTemplates
            .Where(x => x.BankAccount!.IdentifierHash != null
                && ibanHashes.Contains(x.BankAccount.IdentifierHash!)
                && DebitCsvParserKeys.Contains(x.ImportTemplate!.ParserKey))
            .Select(x => new { AccountId = x.BankAccountId, TemplateId = x.ImportTemplateId })
            .Distinct()
            .ToArrayAsync(ct);

        return matches.Length switch
        {
            0 => null,
            1 => (matches[0].AccountId, matches[0].TemplateId),
            _ => throw new InvalidOperationException("Los IBANs del archivo coinciden con más de una variante CSV de débito.")
        };
    }

    public async Task<(Guid AccountId, Guid TemplateId)?> TryResolveAlternativeByIbanPathAsync(
        string relativePath, Guid detectedTemplateId, CancellationToken ct)
    {
        var ibanHashes = ExtractIbanHashes(relativePath);
        if (ibanHashes.Count == 0) return null;
        var match = await db.BankAccountImportTemplates
            .Where(x => x.BankAccount!.IdentifierHash != null
                   && ibanHashes.Contains(x.BankAccount.IdentifierHash!)
                   && x.ImportTemplateId != detectedTemplateId)
            .Select(x => new { AccountId = x.BankAccountId, TemplateId = x.ImportTemplateId })
            .FirstOrDefaultAsync(ct);
        return match is null ? null : (match.AccountId, match.TemplateId);
    }

    public async Task<FinancingAccountPair> ResolveFinancingPairByPathAsync(string relativePath, CancellationToken ct)
    {
        var ibanHashes = ExtractIbanHashes(relativePath);

        if (ibanHashes.Count == 0)
            throw new InvalidOperationException("No se encontraron IBANs en el nombre de carpeta del archivo.");

        var accounts = await db.BankAccounts
            .Where(a => a.IdentifierHash != null && ibanHashes.Contains(a.IdentifierHash))
            .Select(a => new { a.Id, a.CurrencyCode })
            .ToListAsync(ct);

        var crc = accounts.FirstOrDefault(a => a.CurrencyCode == "CRC")?.Id
            ?? throw new InvalidOperationException("No se encontró cuenta CRC para los IBANs del archivo. Verifique que los IBANs estén registrados en el catálogo.");
        var usd = accounts.FirstOrDefault(a => a.CurrencyCode == "USD")?.Id
            ?? throw new InvalidOperationException("No se encontró cuenta USD para los IBANs del archivo. Verifique que los IBANs estén registrados en el catálogo.");

        return new FinancingAccountPair(crc, usd);
    }

    private static HashSet<string> ExtractIbanHashes(string relativePath)
    {
        var folder = relativePath.Replace('\\', '/').Split('/').Reverse().Skip(1).FirstOrDefault() ?? "";
        return IbanPattern.Matches(folder)
            .Select(m => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(m.Value))))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
