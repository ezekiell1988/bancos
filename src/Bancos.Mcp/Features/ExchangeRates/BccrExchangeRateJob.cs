using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bancos.Mcp.Features.ExchangeRates;

[AutomaticRetry(Attempts = 3)]
public sealed class BccrExchangeRateJob(
    McpCatalogDbContext db,
    BccrExchangeRateClient bccr,
    IOptions<BccrOptions> options,
    ILogger<BccrExchangeRateJob> logger)
{
    private readonly BccrOptions options = options.Value;

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task ExecuteAsync(PerformContext? context)
    {
        var bankCodes = options.BankCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (bankCodes.Length == 0)
            throw new InvalidOperationException("BCCR has no target banks configured.");

        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ExchangeRatesModule.CostaRicaTimeZone).DateTime);
        var rate = await bccr.GetSellingRateAsync(localDate);
        var banks = await db.Banks
            .Where(bank => bank.IsEnabled && bankCodes.Contains(bank.Code))
            .ToListAsync();
        if (banks.Count != bankCodes.Length)
            throw new InvalidOperationException("One or more configured BCCR target banks do not exist or are disabled.");

        var bankIds = banks.Select(bank => bank.Id).ToArray();
        var existingRates = await db.ExchangeRates
            .Where(exchangeRate => exchangeRate.RateDate == rate.RateDate && exchangeRate.CurrencyCode == "USD" && bankIds.Contains(exchangeRate.BankId))
            .ToDictionaryAsync(exchangeRate => exchangeRate.BankId);

        foreach (var bank in banks)
        {
            if (existingRates.TryGetValue(bank.Id, out var existingRate))
            {
                existingRate.CrcPerUnit = rate.CrcPerUnit;
                continue;
            }

            db.ExchangeRates.Add(new ExchangeRate
            {
                Id = Guid.NewGuid(),
                BankId = bank.Id,
                RateDate = rate.RateDate,
                CurrencyCode = "USD",
                CrcPerUnit = rate.CrcPerUnit
            });
        }

        await db.SaveChangesAsync();
        context?.WriteLine("Tipo de cambio BCCR actualizado para {0} banco(s), vigencia {1:yyyy-MM-dd}.", banks.Count, rate.RateDate);
        logger.LogInformation("BCCR exchange rate updated for {BankCount} bank(s), date {RateDate}.", banks.Count, rate.RateDate);
    }
}
