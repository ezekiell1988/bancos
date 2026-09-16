using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Bancos.Mcp.Features.ExchangeRates;

[McpServerToolType]
public static class ListExchangeRatesTool
{
    [McpServerTool(
        Name = "list_exchange_rates",
        Title = "Consultar tipos de cambio",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Consulta los tipos de cambio USD registrados para una fecha y, opcionalmente, un banco.")]
    public static async Task<ListExchangeRatesResult> ListAsync(
        [Description("Fecha de vigencia en formato yyyy-MM-dd.")] DateOnly rateDate,
        [Description("Moneda cotizada. Único valor soportado: USD.")] string currencyCode,
        [Description("Código del banco; omitir para consultar todos.")] string? bankCode,
        ExchangeRateService service,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ExchangeRateView> rates;
        try
        {
            rates = await service.ListAsync(rateDate, currencyCode, bankCode, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            throw new McpException(ex.Message);
        }

        return new ListExchangeRatesResult(
            rateDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            currencyCode.ToUpperInvariant(),
            rates.Count == 0,
            rates
                .Select(rate => new ExchangeRateItem(
                    rate.Id,
                    rate.BankCode,
                    rate.BankName,
                    rate.RateDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    rate.CurrencyCode,
                    rate.CrcPerUnit,
                    rate.CreatedAt.ToString("O", CultureInfo.InvariantCulture)))
                .ToList());
    }
}

public sealed record ExchangeRateItem(
    Guid ExchangeRateId,
    string BankCode,
    string BankName,
    string RateDate,
    string CurrencyCode,
    decimal CrcPerUnit,
    string CreatedAt);

public sealed record ListExchangeRatesResult(
    string RateDate,
    string CurrencyCode,
    bool RequiresHumanIntervention,
    IReadOnlyList<ExchangeRateItem> Rates);
