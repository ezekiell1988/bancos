using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Classification;

public sealed class ListUnclassifiedTransactionsTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "list_unclassified_transactions",
        Title: "Listar movimientos No clasificados",
        Description: "Devuelve los movimientos que nunca alcanzaron una clasificación por regla, IA o confirmación manual, "
                   + "junto con la explicación de por qué quedaron pendientes. No incluye datos bancarios sensibles como IBAN o número de tarjeta.",
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
                limit = new
                {
                    type = new[] { "integer", "null" },
                    minimum = 1,
                    maximum = 200,
                    description = "Máximo de movimientos a devolver (por defecto 50)."
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
            required = new[] { "transactions" },
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

        var limit = 50;
        if (arguments.TryGetProperty("limit", out var limitEl) && limitEl.ValueKind == JsonValueKind.Number)
            limit = Math.Clamp(limitEl.GetInt32(), 1, 200);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ClassificationService>();
        var summaries = await service.ListUnclassifiedAsync(bankAccountId, limit, cancellationToken);

        var transactions = summaries.Select(s => new
        {
            transactionId = s.TransactionId,
            bankAccountId = s.BankAccountId,
            transactionDate = s.TransactionDate.ToString("yyyy-MM-dd"),
            description = s.Description,
            amount = s.Amount,
            currencyCode = s.CurrencyCode,
            explanation = s.Explanation
        }).ToList();

        var result = new { transactions };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}
