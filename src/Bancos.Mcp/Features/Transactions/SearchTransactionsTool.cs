using System.Globalization;
using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.Transactions;

public sealed class SearchTransactionsTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "search_transactions",
        Title: "Buscar movimientos",
        Description: "Busca movimientos persistidos filtrando por cuenta bancaria, período, categoría, estado de clasificación y rango de fechas. "
                   + "La respuesta está paginada y no incluye IBAN, números de tarjeta ni credenciales.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                bankAccountId = new
                {
                    type = new[] { "string", "null" },
                    format = "uuid",
                    description = "Limita la búsqueda a una cuenta bancaria específica; omite para incluir todas las cuentas."
                },
                periodId = new
                {
                    type = new[] { "string", "null" },
                    format = "uuid",
                    description = "Limita la búsqueda a un período específico; omite para incluir todos los períodos."
                },
                categoryId = new
                {
                    type = new[] { "string", "null" },
                    format = "uuid",
                    description = "Limita la búsqueda a movimientos cuya clasificación más reciente pertenezca a esta categoría; omite para incluir todas las categorías."
                },
                classificationStatus = new
                {
                    type = new[] { "string", "null" },
                    @enum = new[] { "classified", "unclassified" },
                    description = "Filtra por estado de clasificación: 'classified' (con regla, IA o confirmación manual) o 'unclassified' (sin clasificar). Omite para incluir ambos."
                },
                dateFrom = new
                {
                    type = new[] { "string", "null" },
                    format = "date",
                    description = "Fecha mínima de movimiento (inclusive), formato yyyy-MM-dd."
                },
                dateTo = new
                {
                    type = new[] { "string", "null" },
                    format = "date",
                    description = "Fecha máxima de movimiento (inclusive), formato yyyy-MM-dd."
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
                            bankName = new { type = "string" },
                            accountCode = new { type = "string" },
                            periodId = new { type = new[] { "string", "null" } },
                            periodLabel = new { type = new[] { "string", "null" } },
                            transactionDate = new { type = "string" },
                            description = new { type = "string" },
                            place = new { type = new[] { "string", "null" } },
                            currencyCode = new { type = "string" },
                            amount = new { type = "number" },
                            amountCrc = new { type = "number" },
                            operationType = new { type = "string" },
                            classificationStatus = new { type = "string" },
                            categoryName = new { type = new[] { "string", "null" } }
                        },
                        required = new[]
                        {
                            "transactionId", "bankAccountId", "bankName", "accountCode", "periodId", "periodLabel",
                            "transactionDate", "description", "place", "currencyCode", "amount", "amountCrc",
                            "operationType", "classificationStatus", "categoryName"
                        },
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

        Guid? periodId = null;
        if (arguments.TryGetProperty("periodId", out var periodEl) && periodEl.ValueKind == JsonValueKind.String)
        {
            if (!Guid.TryParse(periodEl.GetString(), out var parsedPeriodId))
                return McpToolResult.Error("'periodId' debe ser un UUID válido.");
            periodId = parsedPeriodId;
        }

        Guid? categoryId = null;
        if (arguments.TryGetProperty("categoryId", out var categoryEl) && categoryEl.ValueKind == JsonValueKind.String)
        {
            if (!Guid.TryParse(categoryEl.GetString(), out var parsedCategoryId))
                return McpToolResult.Error("'categoryId' debe ser un UUID válido.");
            categoryId = parsedCategoryId;
        }

        string? classificationStatus = null;
        if (arguments.TryGetProperty("classificationStatus", out var statusEl) && statusEl.ValueKind == JsonValueKind.String)
        {
            classificationStatus = statusEl.GetString();
            if (classificationStatus is not ("classified" or "unclassified"))
                return McpToolResult.Error("'classificationStatus' debe ser 'classified' o 'unclassified'.");
        }

        DateOnly? dateFrom = null;
        if (arguments.TryGetProperty("dateFrom", out var dateFromEl) && dateFromEl.ValueKind == JsonValueKind.String)
        {
            if (!DateOnly.TryParseExact(dateFromEl.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateFrom))
                return McpToolResult.Error("'dateFrom' debe tener formato yyyy-MM-dd.");
            dateFrom = parsedDateFrom;
        }

        DateOnly? dateTo = null;
        if (arguments.TryGetProperty("dateTo", out var dateToEl) && dateToEl.ValueKind == JsonValueKind.String)
        {
            if (!DateOnly.TryParseExact(dateToEl.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTo))
                return McpToolResult.Error("'dateTo' debe tener formato yyyy-MM-dd.");
            dateTo = parsedDateTo;
        }

        if (dateFrom is not null && dateTo is not null && dateFrom > dateTo)
            return McpToolResult.Error("'dateFrom' no puede ser posterior a 'dateTo'.");

        var page = 1;
        if (arguments.TryGetProperty("page", out var pageEl) && pageEl.ValueKind == JsonValueKind.Number)
            page = Math.Max(pageEl.GetInt32(), 1);

        var itemsPerPage = 50;
        if (arguments.TryGetProperty("itemsPerPage", out var itemsPerPageEl) && itemsPerPageEl.ValueKind == JsonValueKind.Number)
            itemsPerPage = Math.Clamp(itemsPerPageEl.GetInt32(), 1, 200);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TransactionsQueryService>();
        var page_ = await service.SearchAsync(bankAccountId, periodId, categoryId, classificationStatus, dateFrom, dateTo, page, itemsPerPage, cancellationToken);

        var transactions = page_.Items.Select(t => new
        {
            transactionId = t.TransactionId,
            bankAccountId = t.BankAccountId,
            bankName = t.BankName,
            accountCode = t.AccountCode,
            periodId = t.PeriodId,
            periodLabel = t.PeriodLabel,
            transactionDate = t.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            description = t.Description,
            place = t.Place,
            currencyCode = t.CurrencyCode,
            amount = t.Amount,
            amountCrc = t.AmountCrc,
            operationType = t.OperationType,
            classificationStatus = t.ClassificationStatus,
            categoryName = t.CategoryName
        }).ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(page_.TotalItems / (double)page_.ItemsPerPage));
        var result = new { page_.Page, page_.ItemsPerPage, page_.TotalItems, totalPages, transactions };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}
