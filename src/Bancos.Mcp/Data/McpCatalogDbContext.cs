using Bancos.Mcp.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Data;

public sealed class McpCatalogDbContext(DbContextOptions<McpCatalogDbContext> options) : DbContext(options)
{
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankAccountImportTemplate> BankAccountImportTemplates => Set<BankAccountImportTemplate>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<ImportTemplate> ImportTemplates => Set<ImportTemplate>();
    public DbSet<ImportTemplatePattern> ImportTemplatePatterns => Set<ImportTemplatePattern>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Bank>(entity =>
        {
            entity.ToTable("tbBanks", table => table.HasComment("Catálogo de entidades bancarias disponibles para cuentas y tipos de cambio."));
            entity.HasIndex(bank => bank.Code).IsUnique();
            entity.Property(bank => bank.Id).HasColumnName("idBanks").HasComment("Identificador único del banco.");
            entity.Property(bank => bank.Code).HasColumnName("code").HasMaxLength(16).HasComment("Código corto que identifica al banco.");
            entity.Property(bank => bank.Name).HasColumnName("name").HasMaxLength(160).HasComment("Nombre comercial o legal del banco.");
            entity.Property(bank => bank.IsEnabled).HasColumnName("isEnabled").HasComment("Indica si el banco puede usarse en el catálogo.");
            entity.Property(bank => bank.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(bank => bank.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
        });

        builder.Entity<BankAccount>(entity =>
        {
            entity.ToTable("tbBankAccounts", table =>
            {
                table.HasComment("Catálogo de cuentas, tarjetas y préstamos asociados a un banco.");
                table.HasCheckConstraint("CK_tbBankAccounts_accountType", "[accountType] IN ('credit-card', 'debit-card', 'loan')");
                table.HasCheckConstraint("CK_tbBankAccounts_currencyCode", "[currencyCode] IN ('CRC', 'USD')");
            });
            entity.HasIndex(account => new { account.BankId, account.Code }).IsUnique();
            entity.HasIndex(account => account.IdentifierHash).IsUnique().HasFilter("[identifierHash] IS NOT NULL");
            entity.Property(account => account.Id).HasColumnName("idBankAccounts").HasComment("Identificador único de la cuenta bancaria.");
            entity.Property(account => account.BankId).HasColumnName("idBanks").HasComment("Identificador del banco propietario de la cuenta.");
            entity.Property(account => account.Code).HasColumnName("code").HasMaxLength(80).HasComment("Código interno no sensible que identifica la cuenta.");
            entity.Property(account => account.IdentifierHash).HasColumnName("identifierHash").HasMaxLength(64).IsFixedLength().HasComment("Huella criptográfica opcional del identificador bancario normalizado.");
            entity.Property(account => account.CardFingerprint).HasColumnName("cardFingerprint").HasMaxLength(64).IsFixedLength().HasComment("Huella criptográfica opcional de la tarjeta asociada.");
            entity.Property(account => account.AccountType).HasColumnName("accountType").HasMaxLength(16).HasComment("Tipo de producto financiero: tarjeta de crédito, débito o préstamo.");
            entity.Property(account => account.CurrencyCode).HasColumnName("currencyCode").HasMaxLength(3).IsFixedLength().HasComment("Código de moneda permitido para la cuenta.");
            entity.Property(account => account.IsEnabled).HasColumnName("isEnabled").HasComment("Indica si la cuenta puede usarse en el catálogo.");
            entity.Property(account => account.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(account => account.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
            entity.HasOne(account => account.Bank)
                .WithMany(bank => bank.Accounts)
                .HasForeignKey(account => account.BankId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExchangeRate>(entity =>
        {
            entity.ToTable("tbExchangeRates", table =>
            {
                table.HasComment("Tipos de cambio de USD expresados en colones costarricenses por banco y fecha.");
                table.HasCheckConstraint("CK_tbExchangeRates_currencyCode", "[currencyCode] = 'USD'");
                table.HasCheckConstraint("CK_tbExchangeRates_crcPerUnit", "[crcPerUnit] > 0");
            });
            entity.HasIndex(rate => new { rate.BankId, rate.RateDate, rate.CurrencyCode }).IsUnique();
            entity.Property(rate => rate.Id).HasColumnName("idExchangeRates").HasComment("Identificador único del tipo de cambio.");
            entity.Property(rate => rate.BankId).HasColumnName("idBanks").HasComment("Identificador del banco que publica el tipo de cambio.");
            entity.Property(rate => rate.RateDate).HasColumnName("rateDate").HasComment("Fecha de vigencia del tipo de cambio.");
            entity.Property(rate => rate.CurrencyCode).HasColumnName("currencyCode").HasMaxLength(3).IsFixedLength().HasComment("Moneda cotizada; actualmente solo se permite USD.");
            entity.Property(rate => rate.CrcPerUnit).HasColumnName("crcPerUnit").HasPrecision(18, 6).HasComment("Cantidad de colones costarricenses equivalente a una unidad de moneda.");
            entity.Property(rate => rate.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.HasOne(rate => rate.Bank)
                .WithMany(bank => bank.ExchangeRates)
                .HasForeignKey(rate => rate.BankId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BankAccountImportTemplate>(entity =>
        {
            entity.ToTable("tbBankAccountImportTemplates", table => table.HasComment("Relación entre cuentas bancarias y formatos de importación admitidos."));
            entity.HasKey(link => new { link.BankAccountId, link.ImportTemplateId });
            entity.Property(link => link.BankAccountId).HasColumnName("idBankAccounts").HasComment("Identificador de la cuenta bancaria compatible con la plantilla.");
            entity.Property(link => link.ImportTemplateId).HasColumnName("idImportTemplates").HasComment("Identificador de la plantilla compatible con la cuenta.");
            entity.HasOne(link => link.BankAccount)
                .WithMany(account => account.ImportTemplates)
                .HasForeignKey(link => link.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(link => link.ImportTemplate)
                .WithMany(template => template.BankAccounts)
                .HasForeignKey(link => link.ImportTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ImportTemplate>(entity =>
        {
            entity.ToTable("tbImportTemplates", table =>
            {
                table.HasComment("Catálogo de formatos de archivos de importación reconocidos.");
                table.HasCheckConstraint("CK_tbImportTemplates_contentKind", "[contentKind] IN ('csv', 'html', 'xls', 'pdf')");
            });
            entity.HasIndex(template => template.Code).IsUnique();
            entity.Property(template => template.Id).HasColumnName("idImportTemplates").HasComment("Identificador único de la plantilla de importación.");
            entity.Property(template => template.Code).HasColumnName("code").HasMaxLength(80).HasComment("Código estable que identifica la plantilla.");
            entity.Property(template => template.Name).HasColumnName("name").HasMaxLength(160).HasComment("Nombre descriptivo de la plantilla de importación.");
            entity.Property(template => template.ContentKind).HasColumnName("contentKind").HasMaxLength(16).HasComment("Tipo de contenido esperado en el archivo.");
            entity.Property(template => template.ParserKey).HasColumnName("parserKey").HasMaxLength(80).HasComment("Clave del analizador que procesa el formato.");
            entity.Property(template => template.IsEnabled).HasColumnName("isEnabled").HasComment("Indica si la plantilla puede utilizarse para detectar archivos.");
            entity.Property(template => template.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(template => template.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
        });

        builder.Entity<ImportTemplatePattern>(entity =>
        {
            entity.ToTable("tbImportTemplatePatterns", table =>
            {
                table.HasComment("Patrones aprobados para detectar una plantilla de importación por contenido.");
                table.HasCheckConstraint("CK_tbImportTemplatePatterns_definition", "[signatureHash] IS NOT NULL OR [requiredTermsJson] IS NOT NULL");
            });
            entity.HasIndex(pattern => pattern.SignatureHash).IsUnique().HasFilter("[signatureHash] IS NOT NULL");
            entity.Property(pattern => pattern.Id).HasColumnName("idImportTemplatePatterns").HasComment("Identificador único del patrón de detección.");
            entity.Property(pattern => pattern.ImportTemplateId).HasColumnName("idImportTemplates").HasComment("Identificador de la plantilla asociada al patrón.");
            entity.Property(pattern => pattern.SignatureHash).HasColumnName("signatureHash").HasMaxLength(64).IsFixedLength().HasComment("Huella opcional del contenido que identifica el formato.");
            entity.Property(pattern => pattern.PatternKind).HasColumnName("patternKind").HasMaxLength(32).HasComment("Tipo de patrón usado para la detección.");
            entity.Property(pattern => pattern.RequiredTermsJson).HasColumnName("requiredTermsJson").HasComment("Términos que deben existir en el contenido para aceptar el patrón.");
            entity.Property(pattern => pattern.AlternativeTermGroupsJson).HasColumnName("alternativeTermGroupsJson").HasComment("Grupos de términos alternativos aceptados por el patrón.");
            entity.Property(pattern => pattern.DetectorVersion).HasColumnName("detectorVersion").HasComment("Versión del algoritmo de detección asociado.");
            entity.Property(pattern => pattern.IsApproved).HasColumnName("isApproved").HasComment("Indica si el patrón fue aprobado para uso productivo.");
            entity.Property(pattern => pattern.IsActive).HasColumnName("isActive").HasComment("Indica si el patrón está habilitado para detectar archivos.");
            entity.Property(pattern => pattern.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(pattern => pattern.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
            entity.HasOne(pattern => pattern.ImportTemplate)
                .WithMany(template => template.Patterns)
                .HasForeignKey(pattern => pattern.ImportTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedBanks(builder);
        SeedBankAccounts(builder);
        SeedExchangeRates(builder);
        SeedImportTemplates(builder);
        SeedBankAccountImportTemplates(builder);
        SeedImportTemplatePatterns(builder);
    }

    private static void SeedBanks(ModelBuilder builder)
    {
        var createdAt = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.FromHours(-6));
        builder.Entity<Bank>().HasData(
            new Bank { Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), Code = "BCR", Name = "Banco de Costa Rica", CreatedAt = createdAt },
            new Bank { Id = Guid.Parse("30000000-0000-0000-0000-000000000002"), Code = "BN", Name = "Banco Nacional de Costa Rica", CreatedAt = createdAt },
            new Bank { Id = Guid.Parse("30000000-0000-0000-0000-000000000003"), Code = "BAC", Name = "BAC Credomatic", CreatedAt = createdAt },
            new Bank { Id = Guid.Parse("30000000-0000-0000-0000-000000000004"), Code = "COOPEALIANZA", Name = "Coopealianza", CreatedAt = createdAt });
    }

    private static void SeedBankAccounts(ModelBuilder builder)
    {
        var createdAt = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.FromHours(-6));
        var bcr = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var bn = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var bac = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var coopealianza = Guid.Parse("30000000-0000-0000-0000-000000000004");

        builder.Entity<BankAccount>().HasData(
            Account(1, bac, "bac-credit-01-crc", "credit-card", "CRC"),
            Account(2, bac, "bac-credit-01-usd", "credit-card", "USD"),
            Account(3, bac, "bac-credit-02-crc", "credit-card", "CRC"),
            Account(4, bac, "bac-credit-02-usd", "credit-card", "USD"),
            Account(5, bac, "bac-credit-03-crc", "credit-card", "CRC"),
            Account(6, bac, "bac-credit-03-usd", "credit-card", "USD"),
            Account(7, bac, "bac-credit-04-crc", "credit-card", "CRC"),
            Account(8, bac, "bac-credit-04-usd", "credit-card", "USD"),
            Account(9, bn, "bn-credit-01-crc", "credit-card", "CRC"),
            Account(10, bn, "bn-credit-01-usd", "credit-card", "USD"),
            Account(11, bn, "bn-debit-01-usd", "debit-card", "USD"),
            Account(12, bcr, "bcr-debit-01-crc", "debit-card", "CRC"),
            Account(13, bac, "bac-debit-01-crc", "debit-card", "CRC"),
            Account(14, bn, "bn-debit-01-crc", "debit-card", "CRC"),
            Account(15, coopealianza, "coopealianza-loan-01-crc", "loan", "CRC"));

        BankAccount Account(int accountNumber, Guid bankId, string code, string accountType, string currencyCode) => new()
        {
            Id = Guid.Parse($"40000000-0000-0000-0000-{accountNumber:D12}"),
            BankId = bankId,
            Code = code,
            AccountType = accountType,
            CurrencyCode = currencyCode,
            CreatedAt = createdAt
        };
    }

    private static void SeedExchangeRates(ModelBuilder builder)
    {
        var createdAt = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.FromHours(-6));
        var rateDate = new DateOnly(2026, 7, 20);
        builder.Entity<ExchangeRate>().HasData(
            Rate(1, "30000000-0000-0000-0000-000000000002"),
            Rate(2, "30000000-0000-0000-0000-000000000003"));

        ExchangeRate Rate(int rateNumber, string bankId) => new()
        {
            Id = Guid.Parse($"50000000-0000-0000-0000-{rateNumber:D12}"),
            BankId = Guid.Parse(bankId),
            RateDate = rateDate,
            CurrencyCode = "USD",
            CrcPerUnit = 458m,
            CreatedAt = createdAt
        };
    }

    private static void SeedImportTemplates(ModelBuilder builder)
    {
        var createdAt = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.FromHours(-6));
        builder.Entity<ImportTemplate>().HasData(
            new ImportTemplate { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Code = "bcr-debit-csv-v1", Name = "Movimientos de cuenta BCR", ContentKind = "csv", ParserKey = "bcr-debit-csv", CreatedAt = createdAt },
            new ImportTemplate { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Code = "bac-credit-csv-v1", Name = "Resumen de tarjeta BAC", ContentKind = "csv", ParserKey = "bac-credit-csv", CreatedAt = createdAt },
            new ImportTemplate { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Code = "bcr-debit-html-xls-v1", Name = "Movimientos BCR HTML", ContentKind = "html", ParserKey = "bcr-debit-html", CreatedAt = createdAt },
            new ImportTemplate { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Code = "bank-account-movements-xls-v1", Name = "Movimientos de cuenta XLS", ContentKind = "xls", ParserKey = "bank-account-movements-xls", CreatedAt = createdAt },
            new ImportTemplate { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Code = "bac-credit-financing-xls-v1", Name = "Financiamientos BAC", ContentKind = "xls", ParserKey = "bac-credit-financing-xls", CreatedAt = createdAt },
            new ImportTemplate { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Code = "bac-credit-online-pdf-v1", Name = "Tarjeta BAC en linea", ContentKind = "pdf", ParserKey = "bac-credit-online-pdf", CreatedAt = createdAt },
            new ImportTemplate { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Code = "coopealianza-loan-pdf-v1", Name = "Prestamo Coopealianza", ContentKind = "pdf", ParserKey = "coopealianza-loan-pdf", CreatedAt = createdAt },
            new ImportTemplate { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), Code = "bac-account-statement-pdf-v1", Name = "Estado de cuenta consolidado BAC", ContentKind = "pdf", ParserKey = "bac-account-statement-pdf", CreatedAt = createdAt },
            new ImportTemplate { Id = Guid.Parse("10000000-0000-0000-0000-000000000009"), Code = "bn-card-statement-pdf-v1", Name = "Estado de cuenta de tarjeta Banco Nacional", ContentKind = "pdf", ParserKey = "bn-card-statement-pdf", CreatedAt = createdAt });
    }

    private static void SeedBankAccountImportTemplates(ModelBuilder builder)
    {
        var bcrDebit = Guid.Parse("40000000-0000-0000-0000-000000000012");
        var bacCreditAccounts = Enumerable.Range(1, 8)
            .Select(accountNumber => Guid.Parse($"40000000-0000-0000-0000-{accountNumber:D12}"));
        var bnCreditAccounts = Enumerable.Range(9, 2)
            .Select(accountNumber => Guid.Parse($"40000000-0000-0000-0000-{accountNumber:D12}"));
        var bnDebitAccounts = new[]
        {
            Guid.Parse("40000000-0000-0000-0000-000000000011"),
            Guid.Parse("40000000-0000-0000-0000-000000000014")
        };
        var bacDebit = Guid.Parse("40000000-0000-0000-0000-000000000013");
        var coopealianzaLoan = Guid.Parse("40000000-0000-0000-0000-000000000015");

        var links = new List<BankAccountImportTemplate>();
        links.AddRange(Links([bcrDebit], 1, 3));
        links.AddRange(Links(bacCreditAccounts, 2, 5, 6, 8));
        links.AddRange(Links(bnCreditAccounts, 9));
        links.AddRange(Links(bnDebitAccounts, 4));
        links.AddRange(Links([bacDebit], 4));
        links.AddRange(Links([coopealianzaLoan], 7));

        builder.Entity<BankAccountImportTemplate>().HasData(links);

        static IEnumerable<BankAccountImportTemplate> Links(IEnumerable<Guid> bankAccountIds, params int[] templateNumbers) =>
            from bankAccountId in bankAccountIds
            from templateNumber in templateNumbers
            select new BankAccountImportTemplate
            {
                BankAccountId = bankAccountId,
                ImportTemplateId = Guid.Parse($"10000000-0000-0000-0000-{templateNumber:D12}")
            };
    }

    private static void SeedImportTemplatePatterns(ModelBuilder builder)
    {
        var createdAt = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.FromHours(-6));
        var patternKind = "content-terms";
        builder.Entity<ImportTemplatePattern>().HasData(
            Pattern(1, 1, "[\";\",\"oficina\",\"fechamovimiento\",\"numerodocumento\",\"debito\",\"credito\",\"descripcion\"]"),
            Pattern(2, 2, "[\",\",\"name\",\"date\",\"minimum payment\",\"cash payment\",\"local amount\"]", "[[\"dollar amount\",\"dollars amount\"]]"),
            Pattern(3, 3, "[\"banco de costa rica\"]", "[[\"movimientos por rango de fechas\",\"movimientos de la cuenta\",\"movimientos del d\"]]"),
            Pattern(4, 4, "[\"fecha\"]", "[[\"descripcion\",\"detalle\"],[\"debito\",\"debitos\"],[\"credito\",\"creditos\"]]"),
            Pattern(5, 5, "[\"consulta de financiamientos\",\"fecha\",\"concepto\",\"cuotas\",\"monto de cuota\",\"saldo inicial\",\"saldo faltante\"]"),
            Pattern(6, 6, "[\"tarjeta de credito\",\"saldo en colones\",\"saldo en dolares\",\"fecha de pago de contado\"]"),
            Pattern(7, 7, "[\"ver detalles del prestamo\",\"capital\",\"interes\",\"mora\",\"otros\",\"total\",\"saldo\"]"),
            Pattern(8, 8, "[\"numero de tarjeta\",\"marca de tarjeta\",\"plan de lealtad\",\"pagos vencidos\",\"pago de contado\",\"fecha de corte\",\"total pago de contado\"]"),
            Pattern(9, 9, "[\"banco nacional de costa rica\",\"estado de cuenta tarjetas de credito\",\"detalle de compras del periodo\",\"total pago de contado\"]"));

        ImportTemplatePattern Pattern(int patternNumber, int templateNumber, string requiredTermsJson, string? alternativeTermGroupsJson = null) => new()
        {
            Id = Guid.Parse($"20000000-0000-0000-0000-{patternNumber:D12}"),
            ImportTemplateId = Guid.Parse($"10000000-0000-0000-0000-{templateNumber:D12}"),
            PatternKind = patternKind,
            RequiredTermsJson = requiredTermsJson,
            AlternativeTermGroupsJson = alternativeTermGroupsJson,
            IsApproved = true,
            IsActive = true,
            CreatedAt = createdAt
        };
    }
}