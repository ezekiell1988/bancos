using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.ExchangeRates;

[McpServerToolType]
public static class ResolveExchangeRateTool
{
    [McpServerTool(
        Name = "resolve_exchange_rate",
        Title = "Resolver tipo de cambio aplicable",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Resuelve el tipo de cambio USD para una fecha usando la fecha exacta o el último valor anterior disponible; informa si requiere intervención humana.")]
    public static async Task<ResolveExchangeRateResult> ResolveAsync(
        [Description("Fecha solicitada, formato yyyy-MM-dd.")] DateOnly requestedDate,
        [Description("Código de moneda. Único valor soportado: USD.")] string currencyCode,
        [Description("Código del banco; omitir para resolver entre todas las tasas.")] string? bankCode,
        ExchangeRateService service,
        CancellationToken cancellationToken)
    {
        ExchangeRateResolution resolution;
        try
        {
            resolution = await service.ResolveAsync(requestedDate, currencyCode, bankCode, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            throw new McpException(ex.Message);
        }

        return new ResolveExchangeRateResult(
            resolution.Found,
            resolution.IsFallback,
            resolution.RateDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            resolution.CrcPerUnit,
            resolution.CurrencyCode,
            resolution.BankCode,
            resolution.RequiresHumanIntervention,
            resolution.Message,
            resolution.CreatedAt?.ToString("O", CultureInfo.InvariantCulture));
    }
}

public sealed record ResolveExchangeRateResult(
    bool Found,
    bool IsFallback,
    string? RateDate,
    decimal? CrcPerUnit,
    string CurrencyCode,
    string? BankCode,
    bool RequiresHumanIntervention,
    string? Message,
    string? CreatedAt);
