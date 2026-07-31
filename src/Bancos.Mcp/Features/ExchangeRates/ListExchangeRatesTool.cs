using System.Globalization;
using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.ExchangeRates;

public sealed class ListExchangeRatesTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "list_exchange_rates",
        Title: "Consultar tipos de cambio",
        Description: "Consulta los tipos de cambio USD registrados para una fecha y, opcionalmente, un banco.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                rateDate = new { type = "string", format = "date", description = "Fecha de vigencia en formato yyyy-MM-dd." },
                currencyCode = new { type = "string", @enum = new[] { "USD" }, description = "Moneda cotizada." },
                bankCode = new { type = new[] { "string", "null" }, description = "Código del banco; omitir para consultar todos." }
            },
            required = new[] { "rateDate", "currencyCode" },
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                rateDate = new { type = "string" },
                currencyCode = new { type = "string" },
                requiresHumanIntervention = new { type = "boolean" },
                rates = new { type = "array" }
            },
            required = new[] { "rateDate", "currencyCode", "requiresHumanIntervention", "rates" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryReadDate(arguments, "rateDate", out var rateDate, out var dateError))
            return McpToolResult.Error(dateError);
        if (!TryReadString(arguments, "currencyCode", out var currencyCode))
            return McpToolResult.Error("Se requiere 'currencyCode'.");

        var bankCode = arguments.TryGetProperty("bankCode", out var bankElement) && bankElement.ValueKind == JsonValueKind.String
            ? bankElement.GetString()
            : null;

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ExchangeRateService>();
        IReadOnlyList<ExchangeRateView> rates;
        try
        {
            rates = await service.ListAsync(rateDate, currencyCode, bankCode, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return McpToolResult.Error(ex.Message);
        }

        var result = new
        {
            rateDate = rateDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            currencyCode = currencyCode.ToUpperInvariant(),
            requiresHumanIntervention = rates.Count == 0,
            rates = rates.Select(rate => new
            {
                exchangeRateId = rate.Id,
                bankCode = rate.BankCode,
                bankName = rate.BankName,
                rateDate = rate.RateDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                currencyCode = rate.CurrencyCode,
                crcPerUnit = rate.CrcPerUnit,
                createdAt = rate.CreatedAt.ToString("O", CultureInfo.InvariantCulture)
            })
        };
        return JsonResult(result);
    }

    private static bool TryReadDate(JsonElement arguments, string name, out DateOnly date, out string error)
    {
        date = default;
        error = $"Se requiere '{name}' con formato yyyy-MM-dd.";
        return arguments.TryGetProperty(name, out var element)
            && element.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(element.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryReadString(JsonElement arguments, string name, out string value)
    {
        value = string.Empty;
        if (!arguments.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.String)
            return false;
        var text = element.GetString();
        if (string.IsNullOrWhiteSpace(text))
            return false;
        value = text;
        return true;
    }

    private static McpToolResult JsonResult(object result)
    {
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }
}