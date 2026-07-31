using System.Globalization;
using System.Text.Json;
using Bancos.Mcp.Protocol;
using Bancos.Mcp.Tools;

namespace Bancos.Mcp.Features.ExchangeRates;

public sealed class RecordExchangeRateTool(IServiceScopeFactory scopeFactory) : IMcpTool
{
    public McpToolDefinition Definition { get; } = new(
        Name: "record_exchange_rate",
        Title: "Registrar tipo de cambio manual",
        Description: "Registra o corrige un tipo de cambio USD por banco y fecha. La respuesta incluye la operación y la marca temporal de auditoría.",
        InputSchema: new
        {
            type = "object",
            properties = new
            {
                rateDate = new { type = "string", format = "date" },
                currencyCode = new { type = "string", @enum = new[] { "USD" } },
                bankCode = new { type = "string" },
                crcPerUnit = new { type = "number", exclusiveMinimum = 0, description = "Colones por una unidad de USD." }
            },
            required = new[] { "rateDate", "currencyCode", "bankCode", "crcPerUnit" },
            additionalProperties = false
        },
        OutputSchema: new
        {
            type = "object",
            properties = new
            {
                action = new { type = "string" },
                auditSource = new { type = "string" },
                recordedAt = new { type = "string" },
                exchangeRateId = new { type = "string" },
                bankCode = new { type = "string" },
                rateDate = new { type = "string" },
                currencyCode = new { type = "string" },
                crcPerUnit = new { type = "number" }
            },
            required = new[] { "action", "auditSource", "recordedAt", "exchangeRateId", "bankCode", "rateDate", "currencyCode", "crcPerUnit" },
            additionalProperties = false
        });

    public async ValueTask<McpToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryReadDate(arguments, out var rateDate, out var dateError))
            return McpToolResult.Error(dateError);
        if (!TryReadString(arguments, "currencyCode", out var currencyCode))
            return McpToolResult.Error("Se requiere 'currencyCode'.");
        if (!TryReadString(arguments, "bankCode", out var bankCode))
            return McpToolResult.Error("Se requiere 'bankCode'.");
        if (!arguments.TryGetProperty("crcPerUnit", out var valueElement)
            || valueElement.ValueKind != JsonValueKind.Number
            || !valueElement.TryGetDecimal(out var crcPerUnit)
            || crcPerUnit <= 0)
            return McpToolResult.Error("Se requiere 'crcPerUnit' como número mayor que cero.");

        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ExchangeRateService>();
        (Domain.ExchangeRate rate, string action, DateTimeOffset recordedAt) saved;
        try
        {
            saved = await service.RegisterManualAsync(rateDate, currencyCode, bankCode, crcPerUnit, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return McpToolResult.Error(ex.Message);
        }

        var result = new
        {
            action = saved.action,
            auditSource = "manual",
            recordedAt = saved.recordedAt.ToString("O", CultureInfo.InvariantCulture),
            exchangeRateId = saved.rate.Id,
            bankCode = bankCode.ToUpperInvariant(),
            rateDate = saved.rate.RateDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            currencyCode = saved.rate.CurrencyCode,
            crcPerUnit = saved.rate.CrcPerUnit
        };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        return new McpToolResult([McpContent.FromText(json)], result);
    }

    private static bool TryReadDate(JsonElement arguments, out DateOnly date, out string error)
    {
        date = default;
        error = "Se requiere 'rateDate' con formato yyyy-MM-dd.";
        return arguments.TryGetProperty("rateDate", out var element)
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