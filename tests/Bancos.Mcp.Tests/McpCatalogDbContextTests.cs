using Bancos.Mcp.Data;
using Bancos.Mcp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Bancos.Mcp.Tests;

public sealed class McpCatalogDbContextTests
{
    [Fact]
    public void Catalog_schema_uses_descriptive_lower_camel_case_names_and_comments()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new McpCatalogDbContext(options);
        var model = db.GetService<IDesignTimeModel>().Model;

        AssertColumn(model, typeof(Bank), "tbBanks", "Id", "idBanks", "Catálogo de entidades bancarias disponibles para cuentas y tipos de cambio.");
        AssertColumn(model, typeof(BankAccount), "tbBankAccounts", "Id", "idBankAccounts", "Catálogo de cuentas, tarjetas y préstamos asociados a un banco.");
        AssertColumn(model, typeof(ExchangeRate), "tbExchangeRates", "Id", "idExchangeRates", "Tipos de cambio de USD expresados en colones costarricenses por banco y fecha.");
        AssertColumn(model, typeof(ImportTemplate), "tbImportTemplates", "Id", "idImportTemplates", "Catálogo de formatos de archivos de importación reconocidos.");
        AssertColumn(model, typeof(ImportTemplatePattern), "tbImportTemplatePatterns", "Id", "idImportTemplatePatterns", "Patrones aprobados para detectar una plantilla de importación por contenido.");
        AssertColumn(model, typeof(BankAccountImportTemplate), "tbBankAccountImportTemplates", "BankAccountId", "idBankAccounts", "Relación entre cuentas bancarias y formatos de importación admitidos.");
    }

    [Fact]
    public async Task Import_templates_seed_the_known_api_formats()
    {
        var options = new DbContextOptionsBuilder<McpCatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new McpCatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var banks = await db.Banks.OrderBy(bank => bank.Code).ToListAsync();
        var accounts = await db.BankAccounts.OrderBy(account => account.Code).ToListAsync();
        var exchangeRates = await db.ExchangeRates.OrderBy(rate => rate.BankId).ToListAsync();
        var templates = await db.ImportTemplates.OrderBy(template => template.Code).ToListAsync();
        var accountTemplates = await db.BankAccountImportTemplates.ToListAsync();
        var patterns = await db.ImportTemplatePatterns.OrderBy(pattern => pattern.ImportTemplateId).ToListAsync();

        Assert.Equal(4, banks.Count);
        Assert.All(banks, bank => Assert.True(bank.IsEnabled));
        Assert.All(banks, bank => Assert.Equal(TimeSpan.FromHours(-6), bank.CreatedAt.Offset));
        Assert.Contains(banks, bank => bank.Code == "BCR");
        Assert.Contains(banks, bank => bank.Code == "BN");
        Assert.Contains(banks, bank => bank.Code == "BAC");
        Assert.Contains(banks, bank => bank.Code == "COOPEALIANZA");
        Assert.Equal(15, accounts.Count);
        Assert.Equal(10, accounts.Count(account => account.AccountType == "credit-card"));
        Assert.Equal(4, accounts.Count(account => account.AccountType == "debit-card"));
        Assert.Single(accounts, account => account.AccountType == "loan");
        Assert.Equal(9, accounts.Count(account => account.BankId == banks.Single(bank => bank.Code == "BAC").Id));
        Assert.Contains(accounts, account => account.Code == "bac-credit-04-crc" && account.CurrencyCode == "CRC");
        Assert.Contains(accounts, account => account.Code == "bac-credit-04-usd" && account.CurrencyCode == "USD");
        Assert.Contains(accounts, account => account.Code == "bn-debit-01-usd" && account.CurrencyCode == "USD");
        Assert.Equal(2, exchangeRates.Count);
        Assert.All(exchangeRates, rate => Assert.Equal("USD", rate.CurrencyCode));
        Assert.All(exchangeRates, rate => Assert.Equal(458m, rate.CrcPerUnit));
        Assert.All(exchangeRates, rate => Assert.Equal(new DateOnly(2026, 7, 20), rate.RateDate));
        Assert.Contains(exchangeRates, rate => rate.BankId == banks.Single(bank => bank.Code == "BAC").Id);
        Assert.Contains(exchangeRates, rate => rate.BankId == banks.Single(bank => bank.Code == "BN").Id);
        Assert.Equal(40, accountTemplates.Count);
        Assert.Contains(accountTemplates, link => link.BankAccountId == accounts.Single(account => account.Code == "coopealianza-loan-01-crc").Id && link.ImportTemplateId == templates.Single(template => template.Code == "coopealianza-loan-pdf-v1").Id);
        Assert.Equal(9, templates.Count);
        Assert.All(templates, template => Assert.True(template.IsEnabled));
        Assert.All(templates, template => Assert.Equal(TimeSpan.FromHours(-6), template.CreatedAt.Offset));
        Assert.Equal(9, patterns.Count);
        Assert.All(patterns, pattern => Assert.True(pattern.IsApproved && pattern.IsActive));
        Assert.All(patterns, pattern => Assert.Equal("content-terms", pattern.PatternKind));
        Assert.All(patterns, pattern => Assert.Equal(TimeSpan.FromHours(-6), pattern.CreatedAt.Offset));
        Assert.Contains(templates, template => template.Code == "bcr-debit-csv-v1" && template.ContentKind == "csv");
        Assert.Contains(templates, template => template.Code == "bn-card-statement-pdf-v1" && template.ContentKind == "pdf");
    }

    private static void AssertColumn(IModel model, Type entityType, string tableName, string propertyName, string columnName, string tableComment)
    {
        var entity = Assert.IsAssignableFrom<IEntityType>(model.FindEntityType(entityType));
        var storeObject = StoreObjectIdentifier.Table(tableName, null);
        var property = Assert.IsAssignableFrom<IProperty>(entity.FindProperty(propertyName));

        Assert.Equal(tableName, entity.GetTableName());
        Assert.Equal(tableComment, entity.GetComment());
        Assert.Equal(columnName, property.GetColumnName(storeObject));
        Assert.False(string.IsNullOrWhiteSpace(property.GetComment()));
    }
}