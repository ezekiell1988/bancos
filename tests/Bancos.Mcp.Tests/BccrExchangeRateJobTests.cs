using System.Net;
using System.Text;
using Bancos.Mcp.Data;
using Bancos.Mcp.Features.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class BccrExchangeRateJobTests
{
    [Fact]
    public async Task Client_uses_selling_indicator_and_falls_back_to_previous_published_date()
    {
        var handler = new BccrHandler(
            "{\"estado\":true,\"datos\":[{\"series\":[]}]}",
            "{\"estado\":true,\"datos\":[{\"series\":[{\"fecha\":\"2026-07-24\",\"valorDatoPorPeriodo\":519.55}]}]}");
        var client = CreateClient(handler);

        var rate = await client.GetSellingRateAsync(new DateOnly(2026, 7, 25));

        Assert.Equal(new DateOnly(2026, 7, 24), rate.RateDate);
        Assert.Equal(519.55m, rate.CrcPerUnit);
        Assert.Equal(2, handler.RequestUris.Count);
        Assert.All(handler.RequestUris, uri => Assert.Contains("indicadoresEconomicos/318/series", uri));
    }

    [Fact]
    public async Task Job_upserts_rate_once_per_configured_bank_and_date()
    {
        var dbOptions = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new McpCatalogDbContext(dbOptions);
        await db.Database.EnsureCreatedAsync();
        var originalCount = await db.ExchangeRates.CountAsync();
        var client = CreateClient(new BccrHandler(
            "{\"estado\":true,\"datos\":[{\"series\":[{\"fecha\":\"2026-07-27\",\"valorDatoPorPeriodo\":521.25}]}]}"));
        var options = Options.Create(new BccrOptions { Token = "test-token", BankCodes = ["BN", "BAC"] });
        var job = new BccrExchangeRateJob(db, client, options, NullLogger<BccrExchangeRateJob>.Instance);

        await job.ExecuteAsync(null);
        await job.ExecuteAsync(null);

        var currentRates = await db.ExchangeRates
            .Where(rate => rate.RateDate == new DateOnly(2026, 7, 27) && rate.CurrencyCode == "USD")
            .ToListAsync();
        Assert.Equal(originalCount + 2, await db.ExchangeRates.CountAsync());
        Assert.Equal(2, currentRates.Count);
        Assert.All(currentRates, rate => Assert.Equal(521.25m, rate.CrcPerUnit));
    }

    [Fact]
    public void Schedule_is_daily_at_eight_in_costa_rica()
    {
        Assert.Equal("0 8 * * *", ExchangeRatesModule.DailyAtEightInCostaRicaCron);
        Assert.Equal(TimeSpan.FromHours(-6), ExchangeRatesModule.CostaRicaTimeZone.GetUtcOffset(new DateTime(2026, 7, 1)));
    }

    private static BccrExchangeRateClient CreateClient(BccrHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BccrOptions.DefaultBaseUrl)
        };
        return new BccrExchangeRateClient(httpClient, Options.Create(new BccrOptions { Token = "test-token" }));
    }

    private sealed class BccrHandler(params string[] responses) : HttpMessageHandler
    {
        private readonly Queue<string> responses = new(responses);
        private readonly string fallbackResponse = responses.LastOrDefault() ?? "{}";
        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.ToString());
            var content = responses.Count > 0 ? responses.Dequeue() : fallbackResponse;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        }
    }
}
