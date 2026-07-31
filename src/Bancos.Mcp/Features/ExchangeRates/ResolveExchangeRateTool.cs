using System.Globalization;
using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.ExchangeRates;

public sealed class ResolveExchangeRateTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "resolve_exchange_rate",
        Title: "Resolver tipo de cambio aplicable",
        Description: "Resuelve el tipo de cambio USD para una fecha usando la fecha exacta o el último valor anterior disponible; informa si requiere intervención humana.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                requestedDate = new { type = "string", format = "date" },
                currencyCode = new { type = "string", @enum = new[] { "USD" } },
                bankCode = new { type = new[] { "string", "null" }, description = "Código del banco; omitir para resolver entre todas las tasas." }
            },
            required = new[] { "requestedDate", "currencyCode" },
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                found = new { type = "boolean" },
                isFallback = new { type = "boolean" },
                rateDate = new { type = new[] { "string", "null" } },
                crcPerUnit = new { type = new[] { "number", "null" } },
                currencyCode = new { type = "string" },
                bankCode = new { type = new[] { "string", "null" } },
                requiresHumanIntervention = new { type = "boolean" },
                message = new { type = new[] { "string", "null" } },
                createdAt = new { type = new[] { "string", "null" } }
            },
            required = new[] { "found", "isFallback", "rateDate", "crcPerUnit", "currencyCode", "bankCode", "requiresHumanIntervention", "message", "createdAt" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryReadDate(arguments, out var requestedDate, out var dateError))
            return McpToolResult.Error(dateError);
        if (!TryReadString(arguments, "currencyCode", out var currencyCode))
            return McpToolResult.Error("Se requiere 'currencyCode'.");

        var bankCode = arguments.TryGetProperty("bankCode", out var bankElement) && bankElement.ValueKind == JsonValueKind.String
            ? bankElement.GetString()
            : null;

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ExchangeRateService>();
        ExchangeRateResolution resolution;
        try
        {
            resolution = await service.ResolveAsync(requestedDate, currencyCode, bankCode, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return McpToolResult.Error(ex.Message);
        }

        var result = new
        {
            found = resolution.Found,
            isFallback = resolution.IsFallback,
            rateDate = resolution.RateDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            crcPerUnit = resolution.CrcPerUnit,
            currencyCode = resolution.CurrencyCode,
            bankCode = resolution.BankCode,
            requiresHumanIntervention = resolution.RequiresHumanIntervention,
            message = resolution.Message,
            createdAt = resolution.CreatedAt?.ToString("O", CultureInfo.InvariantCulture)
        };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }

    private static bool TryReadDate(JsonElement arguments, out DateOnly date, out string error)
    {
        date = default;
        error = "Se requiere 'requestedDate' con formato yyyy-MM-dd.";
        return arguments.TryGetProperty("requestedDate", out var element)
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
}