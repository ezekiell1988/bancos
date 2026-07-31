using System.Globalization;
using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.CardStatements;

public sealed class ListCardFinancingsTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "list_card_financings",
        Title: "Listar financiamientos de tarjeta",
        Description: "Lista financiamientos activos de tarjetas con saldos, cuotas, tasas y vencimientos. No incluye archivos fuente ni huellas de importación.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                bankAccountId = new { type = new[] { "string", "null" }, format = "uuid", description = "Cuenta de tarjeta a consultar." },
                currencyCode = new { type = new[] { "string", "null" }, @enum = new[] { "CRC", "USD" }, description = "Moneda del financiamiento." },
                page = new { type = new[] { "integer", "null" }, minimum = 1, description = "Página basada en 1; por defecto 1." },
                itemsPerPage = new { type = new[] { "integer", "null" }, minimum = 1, maximum = 200, description = "Cantidad por página; por defecto 50." }
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
                financings = new { type = "array" }
            },
            required = new[] { "page", "itemsPerPage", "totalItems", "totalPages", "financings" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        Guid? bankAccountId = null;
        if (arguments.TryGetProperty("bankAccountId", out var accountElement) && accountElement.ValueKind != JsonValueKind.Null)
        {
            if (accountElement.ValueKind != JsonValueKind.String || !Guid.TryParse(accountElement.GetString(), out var parsedAccountId))
                return McpToolResult.Error("'bankAccountId' debe ser un UUID válido.");
            bankAccountId = parsedAccountId;
        }

        var currencyCode = arguments.TryGetProperty("currencyCode", out var currencyElement) && currencyElement.ValueKind == JsonValueKind.String
            ? currencyElement.GetString()
            : null;
        if (currencyCode is not null && currencyCode is not ("CRC" or "USD"))
            return McpToolResult.Error("'currencyCode' debe ser 'CRC' o 'USD'.");

        var page = arguments.TryGetProperty("page", out var pageElement) && pageElement.ValueKind == JsonValueKind.Number
            ? Math.Max(pageElement.GetInt32(), 1)
            : 1;
        var itemsPerPage = arguments.TryGetProperty("itemsPerPage", out var sizeElement) && sizeElement.ValueKind == JsonValueKind.Number
            ? Math.Clamp(sizeElement.GetInt32(), 1, 200)
            : 50;

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CardStatementsQueryService>();
        var pageResult = await service.ListActiveFinancingsAsync(bankAccountId, currencyCode, page, itemsPerPage, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(pageResult.TotalItems / (double)pageResult.ItemsPerPage));
        var result = new
        {
            pageResult.Page,
            pageResult.ItemsPerPage,
            pageResult.TotalItems,
            totalPages,
            financings = pageResult.Items.Select(financing => new
            {
                cardFinancingId = financing.Id,
                bankAccountId = financing.BankAccountId,
                bankName = financing.BankName,
                accountCode = financing.AccountCode,
                financing.ReferenceNumber,
                financingDate = financing.FinancingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                financing.Concept,
                financing.CurrencyCode,
                financing.InitialBalance,
                financing.OutstandingBalance,
                financing.Installments,
                financing.InstallmentAmount,
                financing.TermMonths,
                financing.AnnualInterestRate,
                dueDate = financing.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                financing.Status
            }).ToList()
        };

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}