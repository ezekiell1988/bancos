using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Accounts;

public sealed class ListBankAccountsTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "list_bank_accounts",
        Title: "Listar cuentas bancarias",
        Description: "Devuelve el catálogo de cuentas bancarias con banco, código, tipo y moneda, paginado. "
                   + "No expone identificadores de negocio como IBAN, número de tarjeta ni credenciales.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                onlyEnabled = new
                {
                    type = new[] { "boolean", "null" },
                    description = "Si es true (por defecto) solo incluye cuentas habilitadas."
                },
                page = new
                {
                    type = new[] { "integer", "null" },
                    minimum = 1,
                    description = "Página a devolver, basada en 1 (por defecto 1)."
                },
                itemsPerPage = new
                {
                    type = new[] { "integer", "null" },
                    minimum = 1,
                    maximum = 200,
                    description = "Cantidad de cuentas por página (por defecto 50; máximo 200)."
                }
            },
            required = Array.Empty<string>(),
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                page = new { type = "integer" },
                itemsPerPage = new { type = "integer" },
                totalItems = new { type = "integer" },
                totalPages = new { type = "integer" },
                accounts = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            bankAccountId = new { type = "string" },
                            bankName = new { type = "string" },
                            bankCode = new { type = "string" },
                            accountCode = new { type = "string" },
                            accountType = new { type = "string" },
                            currencyCode = new { type = "string" },
                            isEnabled = new { type = "boolean" }
                        },
                        required = new[] { "bankAccountId", "bankName", "bankCode", "accountCode", "accountType", "currencyCode", "isEnabled" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "page", "itemsPerPage", "totalItems", "totalPages", "accounts" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var onlyEnabled = true;
        if (arguments.TryGetProperty("onlyEnabled", out var onlyEnabledEl) &&
            (onlyEnabledEl.ValueKind == JsonValueKind.True || onlyEnabledEl.ValueKind == JsonValueKind.False))
            onlyEnabled = onlyEnabledEl.GetBoolean();

        var page = 1;
        if (arguments.TryGetProperty("page", out var pageEl) && pageEl.ValueKind == JsonValueKind.Number)
            page = Math.Max(pageEl.GetInt32(), 1);

        var itemsPerPage = 50;
        if (arguments.TryGetProperty("itemsPerPage", out var itemsPerPageEl) && itemsPerPageEl.ValueKind == JsonValueKind.Number)
            itemsPerPage = Math.Clamp(itemsPerPageEl.GetInt32(), 1, 200);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AccountsQueryService>();
        var page_ = await service.ListBankAccountsAsync(onlyEnabled, page, itemsPerPage, cancellationToken);

        var accounts = page_.Items.Select(a => new
        {
            bankAccountId = a.Id,
            bankName = a.BankName,
            bankCode = a.BankCode,
            accountCode = a.AccountCode,
            accountType = a.AccountType,
            currencyCode = a.CurrencyCode,
            isEnabled = a.IsEnabled
        }).ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(page_.TotalItems / (double)page_.ItemsPerPage));
        var result = new { page_.Page, page_.ItemsPerPage, page_.TotalItems, totalPages, accounts };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}
