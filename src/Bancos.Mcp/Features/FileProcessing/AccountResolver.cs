using Bancos.Mcp.Data;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.FileProcessing;

public sealed class AccountResolver(McpCatalogDbContext db)
{
    public async Task<Guid> ResolveAsync(Guid templateId, Guid? bankAccountId, CancellationToken ct)
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
            .Select(x => x.BankAccountId)
            .ToListAsync(ct);

        return accounts.Count switch
        {
            1 => accounts[0],
            0 => throw new InvalidOperationException("No hay cuentas bancarias vinculadas a esta plantilla."),
            _ => throw new InvalidOperationException($"Hay {accounts.Count} cuentas vinculadas a esta plantilla. Especifique bankAccountId.")
        };
    }
}
