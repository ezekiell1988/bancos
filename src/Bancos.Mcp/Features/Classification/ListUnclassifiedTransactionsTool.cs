using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Classification;

public sealed class ListUnclassifiedTransactionsTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "list_unclassified_transactions",
        Title: "Listar movimientos No clasificados",
        Description: "Devuelve en TOON los movimientos que nunca alcanzaron una clasificación por regla, IA o confirmación manual, "
                   + "junto con la explicación de por qué quedaron pendientes. La respuesta está paginada y no incluye IBAN ni números de tarjeta.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                bankAccountId = new
                {
                    type = new[] { "string", "null" },
                    format = "uuid",
                    description = "Limita el listado a una cuenta bancaria específica; omite para incluir todas las cuentas."
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
                    description = "Cantidad de movimientos por página (por defecto 50; máximo 200)."
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
                transactions = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            transactionId = new { type = "string" },
                            bankAccountId = new { type = "string" },
                            transactionDate = new { type = "string" },
                            description = new { type = "string" },
                            amount = new { type = "number" },
                            currencyCode = new { type = "string" },
                            explanation = new { type = "string" }
                        },
                        required = new[] { "transactionId", "bankAccountId", "transactionDate", "description", "amount", "currencyCode", "explanation" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "page", "itemsPerPage", "totalItems", "totalPages", "transactions" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        Guid? bankAccountId = null;
        if (arguments.TryGetProperty("bankAccountId", out var accountEl) && accountEl.ValueKind == JsonValueKind.String)
        {
            if (!Guid.TryParse(accountEl.GetString(), out var parsedAccountId))
                return McpToolResult.Error("'bankAccountId' debe ser un UUID válido.");
            bankAccountId = parsedAccountId;
        }

        var page = 1;
        if (arguments.TryGetProperty("page", out var pageEl) && pageEl.ValueKind == JsonValueKind.Number)
            page = Math.Max(pageEl.GetInt32(), 1);

        var itemsPerPage = 50;
        if (arguments.TryGetProperty("itemsPerPage", out var itemsPerPageEl) && itemsPerPageEl.ValueKind == JsonValueKind.Number)
            itemsPerPage = Math.Clamp(itemsPerPageEl.GetInt32(), 1, 200);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ClassificationService>();
        var summaries = await service.ListUnclassifiedAsync(bankAccountId, page, itemsPerPage, cancellationToken);

        var transactions = summaries.Items.Select(s => new
        {
            transactionId = s.TransactionId,
            bankAccountId = s.BankAccountId,
            transactionDate = s.TransactionDate.ToString("yyyy-MM-dd"),
            description = s.Description,
            amount = s.Amount,
            currencyCode = s.CurrencyCode,
            explanation = s.Explanation
        }).ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(summaries.TotalItems / (double)summaries.ItemsPerPage));
        var result = new { summaries.Page, summaries.ItemsPerPage, summaries.TotalItems, totalPages, transactions };
        return new McpToolResult([McpContent.FromText(ToonFormatter.Format(summaries))], result);
    }
}
