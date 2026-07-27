using Bancos.Mcp.Data;
using Bancos.Mcp.Features.Parsing;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bancos.Mcp.Features.FileProcessing;

public sealed record FinancingAccountPair(Guid CrcAccountId, Guid UsdAccountId);

public sealed class AccountResolver(McpCatalogDbContext db)
{
    private static readonly Regex IbanPattern = new(@"CR\d{20}", RegexOptions.Compiled);

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

    public async Task<Guid> ResolveCrcByPathAsync(string relativePath, CancellationToken ct)
    {
        var folder = relativePath.Replace('\\', '/').Split('/').Reverse().Skip(1).FirstOrDefault() ?? "";
        var ibanHashes = IbanPattern.Matches(folder)
            .Select(m => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(m.Value))))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (ibanHashes.Count == 0)
            throw new InvalidOperationException("No se encontraron IBANs en el nombre de carpeta del archivo.");

        var account = await db.BankAccounts
            .Where(a => a.IdentifierHash != null && ibanHashes.Contains(a.IdentifierHash) && a.CurrencyCode == "CRC")
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);

        return account ?? throw new InvalidOperationException("No se encontró cuenta CRC para los IBANs del archivo. Verifique que los IBANs estén registrados en el catálogo.");
    }

    public async Task<(Guid AccountId, Guid TemplateId)?> TryResolveAlternativeByIbanPathAsync(
        string relativePath, Guid detectedTemplateId, CancellationToken ct)
    {
        var folder = relativePath.Replace('\\', '/').Split('/').Reverse().Skip(1).FirstOrDefault() ?? "";
        var ibanHashes = IbanPattern.Matches(folder)
            .Select(m => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(m.Value))))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
        var folder = relativePath.Replace('\\', '/').Split('/').Reverse().Skip(1).FirstOrDefault() ?? "";
        var ibanHashes = IbanPattern.Matches(folder)
            .Select(m => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(m.Value))))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
}
