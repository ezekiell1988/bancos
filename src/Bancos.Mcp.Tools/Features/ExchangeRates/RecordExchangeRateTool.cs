using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.ExchangeRates;

[McpServerToolType]
public static class RecordExchangeRateTool
{
    [McpServerTool(
        Name = "record_exchange_rate",
        Title = "Registrar tipo de cambio manual",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Registra o corrige un tipo de cambio USD por banco y fecha. La respuesta incluye la operación y la marca temporal de auditoría.")]
    public static async Task<RecordExchangeRateResult> RecordAsync(
        [Description("Fecha del tipo de cambio, formato yyyy-MM-dd.")] DateOnly rateDate,
        [Description("Código de moneda. Único valor soportado: USD.")] string currencyCode,
        [Description("Código del banco.")] string bankCode,
        [Description("Colones por una unidad de USD, mayor que cero.")] decimal crcPerUnit,
        ExchangeRateService service,
        CancellationToken cancellationToken)
    {
        if (crcPerUnit <= 0)
            throw new McpException("Se requiere 'crcPerUnit' como número mayor que cero.");

        (Domain.ExchangeRate rate, string action, DateTimeOffset recordedAt) saved;
        try
        {
            saved = await service.RegisterManualAsync(rateDate, currencyCode, bankCode, crcPerUnit, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new McpException(ex.Message);
        }

        return new RecordExchangeRateResult(
            saved.action,
            "manual",
            saved.recordedAt,
            saved.rate.Id,
            bankCode.ToUpperInvariant(),
            saved.rate.RateDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            saved.rate.CurrencyCode,
            saved.rate.CrcPerUnit);
    }
}

public sealed record RecordExchangeRateResult(
    string Action,
    string AuditSource,
    DateTimeOffset RecordedAt,
    Guid ExchangeRateId,
    string BankCode,
    string RateDate,
    string CurrencyCode,
    decimal CrcPerUnit);
