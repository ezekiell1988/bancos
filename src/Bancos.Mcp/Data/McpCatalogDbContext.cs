using Bancos.Mcp.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bancos.Mcp.Data;

public sealed class McpCatalogDbContext(DbContextOptions<McpCatalogDbContext> options) : DbContext(options)
{
    public DbSet<ImportTemplate> ImportTemplates => Set<ImportTemplate>();
    public DbSet<ImportTemplatePattern> ImportTemplatePatterns => Set<ImportTemplatePattern>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ImportTemplate>(entity =>
        {
            entity.ToTable("tbImportTemplates", table => table.HasCheckConstraint("CK_tbImportTemplates_ContentKind", "[ContentKind] IN ('csv', 'html', 'xls', 'pdf')"));
            entity.HasIndex(template => template.Code).IsUnique();
            entity.Property(template => template.Code).HasMaxLength(80);
            entity.Property(template => template.Name).HasMaxLength(160);
            entity.Property(template => template.ContentKind).HasMaxLength(16);
            entity.Property(template => template.ParserKey).HasMaxLength(80);
        });

        builder.Entity<ImportTemplatePattern>(entity =>
        {
            entity.ToTable("tbImportTemplatePatterns", table => table.HasCheckConstraint("CK_tbImportTemplatePatterns_Definition", "[SignatureHash] IS NOT NULL OR [RequiredTermsJson] IS NOT NULL"));
            entity.HasIndex(pattern => pattern.SignatureHash).IsUnique().HasFilter("[SignatureHash] IS NOT NULL");
            entity.Property(pattern => pattern.SignatureHash).HasMaxLength(64).IsFixedLength();
            entity.Property(pattern => pattern.PatternKind).HasMaxLength(32);
            entity.HasOne(pattern => pattern.Template)
                .WithMany(template => template.Patterns)
                .HasForeignKey(pattern => pattern.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedImportTemplates(builder);
        SeedImportTemplatePatterns(builder);
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
            TemplateId = Guid.Parse($"10000000-0000-0000-0000-{templateNumber:D12}"),
            PatternKind = patternKind,
            RequiredTermsJson = requiredTermsJson,
            AlternativeTermGroupsJson = alternativeTermGroupsJson,
            IsApproved = true,
            IsActive = true,
            CreatedAt = createdAt
        };
    }
}