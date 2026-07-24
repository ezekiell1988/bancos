using Bancos.Mcp.Data;
using Bancos.Mcp.Features.Parsing;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Bancos.Mcp.Features.FileProcessing;

public sealed class AccountResolver(McpCatalogDbContext db)
{
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
}
