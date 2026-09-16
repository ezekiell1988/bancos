using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.Accounts;

[McpServerToolType]
public static class ListBankAccountsTool
{
    [McpServerTool(Name = "list_bank_accounts", Title = "Listar cuentas bancarias",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Devuelve el catálogo de cuentas bancarias con banco, código, tipo y moneda, paginado. "
               + "No expone identificadores de negocio como IBAN, número de tarjeta ni credenciales.")]
    public static async Task<ListBankAccountsResult> ListBankAccountsAsync(
        AccountsQueryService service,
        [Description("Si es true (por defecto) solo incluye cuentas habilitadas.")] bool onlyEnabled = true,
        [Description("Página a devolver, basada en 1 (por defecto 1).")] int page = 1,
        [Description("Cantidad de cuentas por página (por defecto 50; máximo 200).")] int itemsPerPage = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        itemsPerPage = Math.Clamp(itemsPerPage, 1, 200);

        var page_ = await service.ListBankAccountsAsync(onlyEnabled, page, itemsPerPage, cancellationToken);

        var accounts = page_.Items.Select(a => new BankAccountItem(
            a.Id, a.BankName, a.BankCode, a.AccountCode, a.AccountType, a.CurrencyCode, a.IsEnabled)).ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(page_.TotalItems / (double)page_.ItemsPerPage));
        return new ListBankAccountsResult(page_.Page, page_.ItemsPerPage, page_.TotalItems, totalPages, accounts);
    }
}

public sealed record BankAccountItem(
    Guid BankAccountId,
    string BankName,
    string BankCode,
    string AccountCode,
    string AccountType,
    string CurrencyCode,
    bool IsEnabled);

public sealed record ListBankAccountsResult(
    int Page,
    int ItemsPerPage,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<BankAccountItem> Accounts);
