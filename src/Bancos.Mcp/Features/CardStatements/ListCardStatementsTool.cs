using System.Globalization;
using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.CardStatements;

public sealed class ListCardStatementsTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "list_card_statements",
        Title: "Consultar cortes de tarjeta",
        Description: "Consulta cortes de tarjeta con saldos, fechas de pago y movimientos vinculados. La respuesta no incluye archivos fuente ni huellas de importación.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                bankAccountId = new { type = new[] { "string", "null" }, format = "uuid", description = "Cuenta de tarjeta a consultar." },
                periodLabel = new { type = new[] { "string", "null" }, description = "Etiqueta exacta del período informativo, por ejemplo JUL-2026." },
                statementDateFrom = new { type = new[] { "string", "null" }, format = "date", description = "Fecha mínima de corte, formato yyyy-MM-dd." },
                statementDateTo = new { type = new[] { "string", "null" }, format = "date", description = "Fecha máxima de corte, formato yyyy-MM-dd." },
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
                statements = new { type = "array" }
            },
            required = new[] { "page", "itemsPerPage", "totalItems", "totalPages", "statements" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryGetGuid(arguments, "bankAccountId", out var bankAccountId, out var guidError))
            return McpToolResult.Error(guidError!);
        if (!TryGetDate(arguments, "statementDateFrom", out var statementDateFrom, out var fromError))
            return McpToolResult.Error(fromError!);
        if (!TryGetDate(arguments, "statementDateTo", out var statementDateTo, out var toError))
            return McpToolResult.Error(toError!);
        if (statementDateFrom is not null && statementDateTo is not null && statementDateFrom > statementDateTo)
            return McpToolResult.Error("'statementDateFrom' no puede ser posterior a 'statementDateTo'.");

        var periodLabel = arguments.TryGetProperty("periodLabel", out var periodLabelEl) && periodLabelEl.ValueKind == JsonValueKind.String
            ? periodLabelEl.GetString()
            : null;
        var page = GetPage(arguments);
        var itemsPerPage = GetItemsPerPage(arguments);

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CardStatementsQueryService>();
        var pageResult = await service.ListStatementsAsync(bankAccountId, periodLabel, statementDateFrom, statementDateTo, page, itemsPerPage, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(pageResult.TotalItems / (double)pageResult.ItemsPerPage));
        var result = new
        {
            pageResult.Page,
            pageResult.ItemsPerPage,
            pageResult.TotalItems,
            totalPages,
            statements = pageResult.Items.Select(statement => new
            {
                cardStatementId = statement.Id,
                bankAccountId = statement.BankAccountId,
                bankName = statement.BankName,
                accountCode = statement.AccountCode,
                statementDate = FormatDate(statement.StatementDate),
                statement.PeriodLabel,
                minimumPaymentDueDate = FormatDate(statement.MinimumPaymentDueDate),
                cashPaymentDueDate = FormatDate(statement.CashPaymentDueDate),
                previousBalanceCrc = statement.PreviousBalanceCrc,
                previousBalanceUsd = statement.PreviousBalanceUsd,
                purchasesTotalCrc = statement.PurchasesTotalCrc,
                purchasesTotalUsd = statement.PurchasesTotalUsd,
                paymentsTotalCrc = statement.PaymentsTotalCrc,
                paymentsTotalUsd = statement.PaymentsTotalUsd,
                interestTotalCrc = statement.InterestTotalCrc,
                interestTotalUsd = statement.InterestTotalUsd,
                currentBalanceCrc = statement.CurrentBalanceCrc,
                currentBalanceUsd = statement.CurrentBalanceUsd,
                minimumPaymentCrc = statement.MinimumPaymentCrc,
                minimumPaymentUsd = statement.MinimumPaymentUsd,
                cashPaymentCrc = statement.CashPaymentCrc,
                cashPaymentUsd = statement.CashPaymentUsd,
                creditLimitCrc = statement.CreditLimitCrc,
                creditLimitUsd = statement.CreditLimitUsd,
                availableBalanceCrc = statement.AvailableBalanceCrc,
                availableBalanceUsd = statement.AvailableBalanceUsd,
                lines = statement.Lines.Select(line => new
                {
                    transactionId = line.TransactionId,
                    transactionDate = FormatDate(line.TransactionDate),
                    line.Description,
                    line.Place,
                    line.CurrencyCode,
                    line.Amount,
                    line.AmountCrc,
                    line.OperationType
                }).ToList()
            }).ToList()
        };

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }

    private static bool TryGetGuid(JsonElement arguments, string name, out Guid? value, out string? error)
    {
        value = null;
        error = null;
        if (!arguments.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null)
            return true;
        if (element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var parsed))
        {
            value = parsed;
            return true;
        }

        error = $"'{name}' debe ser un UUID válido.";
        return false;
    }

    private static bool TryGetDate(JsonElement arguments, string name, out DateOnly? value, out string? error)
    {
        value = null;
        error = null;
        if (!arguments.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null)
            return true;
        if (element.ValueKind == JsonValueKind.String && DateOnly.TryParseExact(element.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            value = parsed;
            return true;
        }

        error = $"'{name}' debe tener formato yyyy-MM-dd.";
        return false;
    }

    private static int GetPage(JsonElement arguments) =>
        arguments.TryGetProperty("page", out var element) && element.ValueKind == JsonValueKind.Number
            ? Math.Max(element.GetInt32(), 1)
            : 1;

    private static int GetItemsPerPage(JsonElement arguments) =>
        arguments.TryGetProperty("itemsPerPage", out var element) && element.ValueKind == JsonValueKind.Number
            ? Math.Clamp(element.GetInt32(), 1, 200)
            : 50;

    private static string? FormatDate(DateOnly? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}