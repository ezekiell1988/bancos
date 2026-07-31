using System.Globalization;
using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Transactions;

public sealed class GetTransactionDetailTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "get_transaction_detail",
        Title: "Detalle de movimiento",
        Description: "Devuelve el detalle de un movimiento con su historial completo de clasificación (regla, IA, manual o sin clasificar) "
                   + "para trazabilidad. No incluye IBAN, número de tarjeta ni credenciales.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                transactionId = new
                {
                    type = "string",
                    format = "uuid",
                    description = "ID del movimiento a consultar."
                }
            },
            required = new[] { "transactionId" },
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                transactionId = new { type = "string" },
                bankAccountId = new { type = "string" },
                bankName = new { type = "string" },
                accountCode = new { type = "string" },
                periodId = new { type = new[] { "string", "null" } },
                periodLabel = new { type = new[] { "string", "null" } },
                referenceNumber = new { type = new[] { "string", "null" } },
                transactionDate = new { type = "string" },
                paymentDate = new { type = new[] { "string", "null" } },
                description = new { type = "string" },
                place = new { type = new[] { "string", "null" } },
                currencyCode = new { type = "string" },
                amount = new { type = "number" },
                amountCrc = new { type = "number" },
                exchangeRate = new { type = new[] { "number", "null" } },
                operationType = new { type = "string" },
                classifications = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string" },
                            source = new { type = "string" },
                            categoryCode = new { type = new[] { "string", "null" } },
                            categoryName = new { type = new[] { "string", "null" } },
                            confidence = new { type = new[] { "number", "null" } },
                            explanation = new { type = new[] { "string", "null" } },
                            createdAt = new { type = "string" },
                            ruleDescriptionPattern = new { type = new[] { "string", "null" } }
                        },
                        required = new[] { "id", "source", "categoryCode", "categoryName", "confidence", "explanation", "createdAt", "ruleDescriptionPattern" },
                        additionalProperties = false
                    }
                }
            },
            required = new[]
            {
                "transactionId", "bankAccountId", "bankName", "accountCode", "periodId", "periodLabel",
                "referenceNumber", "transactionDate", "paymentDate", "description", "place", "currencyCode",
                "amount", "amountCrc", "exchangeRate", "operationType", "classifications"
            },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("transactionId", out var transactionIdEl) ||
            !Guid.TryParse(transactionIdEl.GetString(), out var transactionId))
            return McpToolResult.Error("Se requiere 'transactionId' como UUID válido.");

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TransactionsQueryService>();
        var detail = await service.GetDetailAsync(transactionId, cancellationToken);
        if (detail is null)
            return McpToolResult.Error($"Movimiento {transactionId} no encontrado.");

        var result = new
        {
            transactionId = detail.TransactionId,
            bankAccountId = detail.BankAccountId,
            bankName = detail.BankName,
            accountCode = detail.AccountCode,
            periodId = detail.PeriodId,
            periodLabel = detail.PeriodLabel,
            referenceNumber = detail.ReferenceNumber,
            transactionDate = detail.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            paymentDate = detail.PaymentDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            description = detail.Description,
            place = detail.Place,
            currencyCode = detail.CurrencyCode,
            amount = detail.Amount,
            amountCrc = detail.AmountCrc,
            exchangeRate = detail.ExchangeRate,
            operationType = detail.OperationType,
            classifications = detail.Classifications.Select(c => new
            {
                id = c.Id,
                source = c.Source,
                categoryCode = c.CategoryCode,
                categoryName = c.CategoryName,
                confidence = c.Confidence,
                explanation = c.Explanation,
                createdAt = c.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                ruleDescriptionPattern = c.RuleDescriptionPattern
            }).ToList()
        };

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}
