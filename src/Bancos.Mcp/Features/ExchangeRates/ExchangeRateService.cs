using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Features.ExchangeRates;

public sealed record ExchangeRateView(
    Guid Id,
    string BankCode,
    string BankName,
    DateOnly RateDate,
    string CurrencyCode,
    decimal CrcPerUnit,
    DateTimeOffset CreatedAt);

public sealed record ExchangeRateResolution(
    bool Found,
    bool IsFallback,
    DateOnly? RateDate,
    decimal? CrcPerUnit,
    string CurrencyCode,
    string? BankCode,
    bool RequiresHumanIntervention,
    string? Message,
    DateTimeOffset? CreatedAt);

public sealed class ExchangeRateService(McpCatalogDbContext db)
{
    public async Task<IReadOnlyList<ExchangeRateView>> ListAsync(
        DateOnly rateDate,
        string currencyCode,
        string? bankCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCurrency = NormalizeCurrencyCode(currencyCode);
        var normalizedBank = NormalizeBankCode(bankCode);
        var query = db.ExchangeRates
            .AsNoTracking()
            .Include(rate => rate.Bank)
            .Where(rate => rate.RateDate == rateDate && rate.CurrencyCode == normalizedCurrency);

        if (normalizedBank is not null)
            query = query.Where(rate => rate.Bank!.Code == normalizedBank);

        return await query
            .OrderBy(rate => rate.Bank!.Code)
            .Select(rate => new ExchangeRateView(
                rate.Id,
                rate.Bank!.Code,
                rate.Bank.Name,
                rate.RateDate,
                rate.CurrencyCode,
                rate.CrcPerUnit,
                rate.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<(ExchangeRate Rate, string Action, DateTimeOffset RecordedAt)> RegisterManualAsync(
        DateOnly rateDate,
        string currencyCode,
        string bankCode,
        decimal crcPerUnit,
        CancellationToken cancellationToken = default)
    {
        var normalizedCurrency = NormalizeCurrencyCode(currencyCode);
        var normalizedBank = NormalizeBankCode(bankCode)
            ?? throw new ArgumentException("Se requiere un código de banco.", nameof(bankCode));
        if (crcPerUnit <= 0)
            throw new ArgumentException("crcPerUnit debe ser mayor que cero.", nameof(crcPerUnit));

        var bank = await db.Banks
            .SingleOrDefaultAsync(candidate => candidate.Code == normalizedBank && candidate.IsEnabled, cancellationToken)
            ?? throw new InvalidOperationException($"El banco habilitado '{normalizedBank}' no existe.");

        var existing = await db.ExchangeRates
            .SingleOrDefaultAsync(rate => rate.BankId == bank.Id
                && rate.RateDate == rateDate
                && rate.CurrencyCode == normalizedCurrency, cancellationToken);
        var recordedAt = CostaRicaTime.Now;

        if (existing is not null)
        {
            existing.CrcPerUnit = crcPerUnit;
            await db.SaveChangesAsync(cancellationToken);
            return (existing, "updated", recordedAt);
        }

        var rate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            BankId = bank.Id,
            RateDate = rateDate,
            CurrencyCode = normalizedCurrency,
            CrcPerUnit = crcPerUnit,
            CreatedAt = recordedAt
        };
        db.ExchangeRates.Add(rate);
        await db.SaveChangesAsync(cancellationToken);
        return (rate, "created", recordedAt);
    }

    public async Task<ExchangeRateResolution> ResolveAsync(
        DateOnly requestedDate,
        string currencyCode,
        string? bankCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCurrency = NormalizeCurrencyCode(currencyCode);
        var normalizedBank = NormalizeBankCode(bankCode);
        var query = db.ExchangeRates
            .AsNoTracking()
            .Include(rate => rate.Bank)
            .Where(rate => rate.CurrencyCode == normalizedCurrency && rate.RateDate <= requestedDate);

        if (normalizedBank is not null)
            query = query.Where(rate => rate.Bank!.Code == normalizedBank);

        var rate = await query
            .OrderByDescending(candidate => candidate.RateDate)
            .ThenBy(candidate => candidate.Bank!.Code)
            .FirstOrDefaultAsync(cancellationToken);

        if (rate is null)
        {
            return new ExchangeRateResolution(
                Found: false,
                IsFallback: false,
                RateDate: null,
                CrcPerUnit: null,
                CurrencyCode: normalizedCurrency,
                BankCode: normalizedBank,
                RequiresHumanIntervention: true,
                Message: "No existe un tipo de cambio aplicable; se requiere registro o revisión manual.",
                CreatedAt: null);
        }

        return new ExchangeRateResolution(
            Found: true,
            IsFallback: rate.RateDate != requestedDate,
            RateDate: rate.RateDate,
            CrcPerUnit: rate.CrcPerUnit,
            CurrencyCode: rate.CurrencyCode,
            BankCode: rate.Bank!.Code,
            RequiresHumanIntervention: false,
            Message: rate.RateDate == requestedDate ? null : "Se aplicó el último tipo de cambio anterior disponible.",
            CreatedAt: rate.CreatedAt);
    }

    private static string NormalizeCurrencyCode(string currencyCode)
    {
        if (!string.Equals(currencyCode?.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("currencyCode debe ser 'USD'.", nameof(currencyCode));
        return "USD";
    }

    private static string? NormalizeBankCode(string? bankCode) =>
        string.IsNullOrWhiteSpace(bankCode) ? null : bankCode.Trim().ToUpperInvariant();
}