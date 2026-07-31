using System.Text.Json;
using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Bancos.Mcp.Features.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class ExchangeRateToolsTests
{
    [Fact]
    public async Task Resolve_returns_exact_rate_when_date_exists()
    {
        await using var db = CreateDb();
        var bank = await SeedBankAsync(db, "BN");
        await AddRateAsync(db, bank, new DateOnly(2026, 7, 31), 458m);
        var service = new ExchangeRateService(db);

        var result = await service.ResolveAsync(new DateOnly(2026, 7, 31), "USD", "BN");

        Assert.True(result.Found);
        Assert.False(result.IsFallback);
        Assert.Equal(458m, result.CrcPerUnit);
        Assert.False(result.RequiresHumanIntervention);
    }

    [Fact]
    public async Task Resolve_uses_previous_rate_and_marks_fallback()
    {
        await using var db = CreateDb();
        var bank = await SeedBankAsync(db, "BN");
        await AddRateAsync(db, bank, new DateOnly(2026, 7, 30), 458m);
        var service = new ExchangeRateService(db);

        var result = await service.ResolveAsync(new DateOnly(2026, 7, 31), "USD", "BN");

        Assert.True(result.Found);
        Assert.True(result.IsFallback);
        Assert.Equal(new DateOnly(2026, 7, 30), result.RateDate);
        Assert.Equal(458m, result.CrcPerUnit);
    }

    [Fact]
    public async Task Resolve_reports_human_intervention_when_no_rate_exists()
    {
        await using var db = CreateDb();
        await SeedBankAsync(db, "BN");
        var service = new ExchangeRateService(db);

        var result = await service.ResolveAsync(new DateOnly(2026, 7, 31), "USD", "BN");

        Assert.False(result.Found);
        Assert.True(result.RequiresHumanIntervention);
        Assert.Null(result.CrcPerUnit);
    }

    [Fact]
    public async Task Manual_registration_creates_then_corrects_rate()
    {
        await using var db = CreateDb();
        await SeedBankAsync(db, "BN");
        var service = new ExchangeRateService(db);
        var date = new DateOnly(2026, 7, 31);

        var created = await service.RegisterManualAsync(date, "USD", "BN", 458m);
        var updated = await service.RegisterManualAsync(date, "USD", "BN", 459m);

        Assert.Equal("created", created.Action);
        Assert.Equal("updated", updated.Action);
        Assert.Equal(created.Rate.Id, updated.Rate.Id);
        Assert.Equal(459m, updated.Rate.CrcPerUnit);
        Assert.Equal(1, await db.ExchangeRates.CountAsync());
    }

    [Fact]
    public async Task Resolve_tool_returns_structured_fallback_result()
    {
        await using var db = CreateDb();
        var bank = await SeedBankAsync(db, "BN");
        await AddRateAsync(db, bank, new DateOnly(2026, 7, 30), 458m);
        var scopeFactory = new TestScopeFactory(db);
        var tool = new ResolveExchangeRateTool(scopeFactory);
        using var arguments = JsonDocument.Parse("""{"requestedDate":"2026-07-31","currencyCode":"USD","bankCode":"BN"}""");

        var result = await tool.ExecuteAsync(arguments.RootElement, CancellationToken.None);

        Assert.True(result.StructuredContent is not null);
        using var structured = JsonDocument.Parse(JsonSerializer.Serialize(result.StructuredContent));
        Assert.True(structured.RootElement.GetProperty("isFallback").GetBoolean());
        Assert.Equal(458m, structured.RootElement.GetProperty("crcPerUnit").GetDecimal());
    }

    private static McpCatalogDbContext CreateDb() => new(new DbContextOptionsBuilder<McpCatalogDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static async Task<Bank> SeedBankAsync(McpCatalogDbContext db, string code)
    {
        var bank = new Bank { Id = Guid.NewGuid(), Code = code, Name = code };
        db.Banks.Add(bank);
        await db.SaveChangesAsync();
        return bank;
    }

    private static async Task AddRateAsync(McpCatalogDbContext db, Bank bank, DateOnly date, decimal value)
    {
        db.ExchangeRates.Add(new ExchangeRate { Id = Guid.NewGuid(), BankId = bank.Id, RateDate = date, CurrencyCode = "USD", CrcPerUnit = value });
        await db.SaveChangesAsync();
    }

    private sealed class TestScopeFactory(McpCatalogDbContext db) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new TestScope(db);
    }

    private sealed class TestScope(McpCatalogDbContext db) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new TestServiceProvider(db);
        public void Dispose() { }
    }

    private sealed class TestServiceProvider(McpCatalogDbContext db) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(ExchangeRateService) ? new ExchangeRateService(db) : null;
    }
}