using System.Net.Http.Headers;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Bancos.Mcp.Features.ExchangeRates;

public static class ExchangeRatesModule
{
    public const string DailyAtEightInCostaRicaCron = "0 8 * * *";
    public static readonly TimeZoneInfo CostaRicaTimeZone = FindCostaRicaTimeZone();

    public static IServiceCollection AddExchangeRatesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BccrOptions>().Bind(configuration.GetSection(BccrOptions.Section));
        services.AddHttpClient<BccrExchangeRateClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BccrOptions>>().Value;
            client.BaseAddress = new Uri(BccrOptions.DefaultBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(options.Token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);
        });
        services.AddScoped<BccrExchangeRateJob>();
        return services;
    }

    public static IApplicationBuilder UseExchangeRatesJobs(this IApplicationBuilder app)
    {
        RecurringJob.AddOrUpdate<BccrExchangeRateJob>(
            "refresh-bccr-exchange-rates",
            job => job.ExecuteAsync(null!),
            DailyAtEightInCostaRicaCron,
            new RecurringJobOptions { TimeZone = CostaRicaTimeZone });
        return app;
    }

    private static TimeZoneInfo FindCostaRicaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time"); }
    }
}
