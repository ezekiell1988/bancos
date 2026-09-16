using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Bancos.Mcp.Features.ExchangeRates;

public sealed class BccrExchangeRateClient(HttpClient httpClient, IOptions<BccrOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly BccrOptions options = options.Value;

    public async Task<BccrExchangeRate> GetSellingRateAsync(DateOnly requestedDate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Token))
            throw new InvalidOperationException("BCCR credentials are not configured.");

        for (var offset = 0; offset <= 3; offset++)
        {
            var date = requestedDate.AddDays(-offset);
            var formattedDate = Uri.EscapeDataString(date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture));
            var uri = $"indicadoresEconomicos/{options.DollarIndicator}/series?fechaInicio={formattedDate}&fechaFin={formattedDate}&idioma=ES";

            using var response = await httpClient.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<BccrSeriesResponse>(JsonOptions, cancellationToken);
            if (payload is null || !payload.Estado)
                throw new InvalidOperationException("BCCR returned an invalid exchange-rate response.");

            var series = payload.Datos?
                .SelectMany(data => data.Series ?? [])
                .FirstOrDefault(item => item.Value.HasValue && !string.IsNullOrWhiteSpace(item.Date));
            if (series?.Value is not decimal value || !DateOnly.TryParse(series.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var rateDate))
                continue;

            if (value <= 0)
                throw new InvalidOperationException("BCCR returned an invalid exchange-rate value.");

            return new BccrExchangeRate(rateDate, value);
        }

        throw new InvalidOperationException("BCCR has no published selling exchange rate for the requested date.");
    }

    private sealed class BccrSeriesResponse
    {
        [JsonPropertyName("estado")]
        public bool Estado { get; init; }

        [JsonPropertyName("datos")]
        public List<BccrIndicatorData>? Datos { get; init; }
    }

    private sealed class BccrIndicatorData
    {
        [JsonPropertyName("series")]
        public List<BccrSeriesItem>? Series { get; init; }
    }

    private sealed class BccrSeriesItem
    {
        [JsonPropertyName("fecha")]
        public string? Date { get; init; }

        [JsonPropertyName("valorDatoPorPeriodo")]
        public decimal? Value { get; init; }
    }
}

public sealed record BccrExchangeRate(DateOnly RateDate, decimal CrcPerUnit);
