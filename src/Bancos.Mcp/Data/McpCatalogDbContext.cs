using Bancos.Mcp.Domain;
using Bancos.Mcp.Catalog;
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
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<CardStatement> CardStatements => Set<CardStatement>();
    public DbSet<CardStatementLine> CardStatementLines => Set<CardStatementLine>();
    public DbSet<CardFinancing> CardFinancings => Set<CardFinancing>();
    public DbSet<LoanStatement> LoanStatements => Set<LoanStatement>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();

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

        builder.Entity<Period>(entity =>
        {
            entity.ToTable("tbPeriods", table => table.HasComment("Períodos de reporte financiero. Cada período corre del 19 de un mes al 18 del siguiente."));
            entity.HasIndex(p => p.Label).IsUnique();
            entity.HasIndex(p => p.StartDate).IsUnique();
            entity.HasIndex(p => p.EndDate).IsUnique();
            entity.Property(p => p.Id).HasColumnName("idPeriods").HasComment("Identificador único del período.");
            entity.Property(p => p.Label).HasColumnName("label").HasMaxLength(20).HasComment("Nombre visible del período, ej. JUL-2026.");
            entity.Property(p => p.StartDate).HasColumnName("startDate").HasComment("Fecha de inicio del período (día 19 del mes anterior).");
            entity.Property(p => p.EndDate).HasColumnName("endDate").HasComment("Fecha de cierre del período (día 18 del mes en curso).");
            entity.Property(p => p.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(p => p.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
        });

        builder.Entity<Transaction>(entity =>
        {
            entity.ToTable("tbTransactions", table =>
            {
                table.HasComment("Movimientos individuales extraídos de estados de cuenta.");
                table.HasCheckConstraint("CK_tbTransactions_currencyCode", "[currencyCode] IN ('CRC', 'USD')");
                table.HasCheckConstraint("CK_tbTransactions_operationType", "[operationType] IN ('purchase', 'payment', 'interest', 'other-charge', 'interest-reversal')");
            });
            entity.HasIndex(t => new { t.BankAccountId, t.SourceFingerprint }).IsUnique();
            entity.Property(t => t.Id).HasColumnName("idTransactions").HasComment("Identificador único del movimiento.");
            entity.Property(t => t.BankAccountId).HasColumnName("idBankAccounts").HasComment("Cuenta bancaria origen del movimiento.");
            entity.Property(t => t.PeriodId).HasColumnName("idPeriods").HasComment("Período de reporte; null si aún no se ha creado el período.");
            entity.Property(t => t.ReferenceNumber).HasColumnName("referenceNumber").HasMaxLength(40).HasComment("N. Referencia del extracto.");
            entity.Property(t => t.TransactionDate).HasColumnName("transactionDate").HasComment("Fecha de la transacción.");
            entity.Property(t => t.PaymentDate).HasColumnName("paymentDate").HasComment("Fecha de pago, si aplica.");
            entity.Property(t => t.Description).HasColumnName("description").HasMaxLength(200).HasComment("Concepto o descripción del movimiento.");
            entity.Property(t => t.Place).HasColumnName("place").HasMaxLength(120).HasComment("Lugar o comercio donde se realizó la transacción.");
            entity.Property(t => t.CurrencyCode).HasColumnName("currencyCode").HasMaxLength(3).IsFixedLength().HasComment("Moneda de la transacción.");
            entity.Property(t => t.Amount).HasColumnName("amount").HasPrecision(18, 2).HasComment("Monto original; positivo=cargo, negativo=abono.");
            entity.Property(t => t.AmountCrc).HasColumnName("amountCrc").HasPrecision(18, 2).HasComment("Monto convertido a colones.");
            entity.Property(t => t.ExchangeRate).HasColumnName("exchangeRate").HasPrecision(18, 6).HasComment("Tipo de cambio usado para la conversión.");
            entity.Property(t => t.OperationType).HasColumnName("operationType").HasMaxLength(32).HasComment("Tipo de operación.");
            entity.Property(t => t.SourceFingerprint).HasColumnName("sourceFingerprint").HasMaxLength(64).IsFixedLength().HasComment("SHA-256 para deduplicación.");
            entity.Property(t => t.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(t => t.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
            entity.HasOne(t => t.BankAccount)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Period)
                .WithMany(p => p.Transactions)
                .HasForeignKey(t => t.PeriodId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<CardStatement>(entity =>
        {
            entity.ToTable("tbCardStatements", table => table.HasComment("Header del corte mensual de tarjeta de crédito con totales del período."));
            entity.HasIndex(cs => new { cs.BankAccountId, cs.StatementDate }).IsUnique();
            entity.Property(cs => cs.Id).HasColumnName("idCardStatements").HasComment("Identificador único del corte.");
            entity.Property(cs => cs.BankAccountId).HasColumnName("idBankAccounts").HasComment("Tarjeta de crédito asociada al corte.");
            entity.Property(cs => cs.StatementDate).HasColumnName("statementDate").HasComment("Fecha de corte.");
            entity.Property(cs => cs.PeriodLabel).HasColumnName("periodLabel").HasMaxLength(20).HasComment("Período informativo del header, ej. JUL-2026.");
            entity.Property(cs => cs.MinimumPaymentDueDate).HasColumnName("minimumPaymentDueDate").HasComment("Fecha límite pago mínimo.");
            entity.Property(cs => cs.CashPaymentDueDate).HasColumnName("cashPaymentDueDate").HasComment("Fecha límite pago de contado.");
            entity.Property(cs => cs.PreviousBalanceCrc).HasColumnName("previousBalanceCrc").HasPrecision(18, 2);
            entity.Property(cs => cs.PreviousBalanceUsd).HasColumnName("previousBalanceUsd").HasPrecision(18, 2);
            entity.Property(cs => cs.PurchasesTotalCrc).HasColumnName("purchasesTotalCrc").HasPrecision(18, 2);
            entity.Property(cs => cs.PurchasesTotalUsd).HasColumnName("purchasesTotalUsd").HasPrecision(18, 2);
            entity.Property(cs => cs.PaymentsTotalCrc).HasColumnName("paymentsTotalCrc").HasPrecision(18, 2);
            entity.Property(cs => cs.PaymentsTotalUsd).HasColumnName("paymentsTotalUsd").HasPrecision(18, 2);
            entity.Property(cs => cs.InterestTotalCrc).HasColumnName("interestTotalCrc").HasPrecision(18, 2);
            entity.Property(cs => cs.InterestTotalUsd).HasColumnName("interestTotalUsd").HasPrecision(18, 2);
            entity.Property(cs => cs.CurrentBalanceCrc).HasColumnName("currentBalanceCrc").HasPrecision(18, 2);
            entity.Property(cs => cs.CurrentBalanceUsd).HasColumnName("currentBalanceUsd").HasPrecision(18, 2);
            entity.Property(cs => cs.MinimumPaymentCrc).HasColumnName("minimumPaymentCrc").HasPrecision(18, 2);
            entity.Property(cs => cs.MinimumPaymentUsd).HasColumnName("minimumPaymentUsd").HasPrecision(18, 2);
            entity.Property(cs => cs.CashPaymentCrc).HasColumnName("cashPaymentCrc").HasPrecision(18, 2);
            entity.Property(cs => cs.CashPaymentUsd).HasColumnName("cashPaymentUsd").HasPrecision(18, 2);
            entity.Property(cs => cs.CreditLimitCrc).HasColumnName("creditLimitCrc").HasPrecision(18, 2);
            entity.Property(cs => cs.CreditLimitUsd).HasColumnName("creditLimitUsd").HasPrecision(18, 2);
            entity.Property(cs => cs.AvailableBalanceCrc).HasColumnName("availableBalanceCrc").HasPrecision(18, 2);
            entity.Property(cs => cs.AvailableBalanceUsd).HasColumnName("availableBalanceUsd").HasPrecision(18, 2);
            entity.Property(cs => cs.SourceFingerprint).HasColumnName("sourceFingerprint").HasMaxLength(64).IsFixedLength().HasComment("SHA-256 para deduplicación.");
            entity.Property(cs => cs.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(cs => cs.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
            entity.HasOne(cs => cs.BankAccount)
                .WithMany(a => a.CardStatements)
                .HasForeignKey(cs => cs.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CardStatementLine>(entity =>
        {
            entity.ToTable("tbCardStatementLines", table => table.HasComment("Auxiliar que asocia movimientos a un corte de tarjeta. Surrogate PK + UNIQUE constraint per ADR-03."));
            entity.HasIndex(l => new { l.CardStatementId, l.TransactionId }).IsUnique();
            entity.HasIndex(l => l.TransactionId);
            entity.Property(l => l.Id).HasColumnName("idCardStatementLines").HasComment("Identificador único de la línea.");
            entity.Property(l => l.CardStatementId).HasColumnName("idCardStatements").HasComment("Corte al que pertenece el movimiento.");
            entity.Property(l => l.TransactionId).HasColumnName("idTransactions").HasComment("Movimiento incluido en el corte.");
            entity.Property(l => l.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.HasOne(l => l.CardStatement)
                .WithMany(cs => cs.Lines)
                .HasForeignKey(l => l.CardStatementId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(l => l.Transaction)
                .WithMany(t => t.CardStatementLines)
                .HasForeignKey(l => l.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CardFinancing>(entity =>
        {
            entity.ToTable("tbCardFinancings", table =>
            {
                table.HasComment("Planes de cuotas y financiamientos activos en tarjeta (snapshot del estado actual).");
                table.HasCheckConstraint("CK_tbCardFinancings_currencyCode", "[currencyCode] IN ('CRC', 'USD')");
                table.HasCheckConstraint("CK_tbCardFinancings_status", "[status] IN ('active', 'cancelled', 'settled')");
            });
            entity.HasIndex(cf => new { cf.BankAccountId, cf.SourceFingerprint }).IsUnique();
            entity.Property(cf => cf.Id).HasColumnName("idCardFinancings").HasComment("Identificador único del financiamiento.");
            entity.Property(cf => cf.BankAccountId).HasColumnName("idBankAccounts").HasComment("Tarjeta de crédito asociada.");
            entity.Property(cf => cf.ReferenceNumber).HasColumnName("referenceNumber").HasMaxLength(40).HasComment("Número de referencia del financiamiento.");
            entity.Property(cf => cf.FinancingDate).HasColumnName("financingDate").HasComment("Fecha del financiamiento.");
            entity.Property(cf => cf.Concept).HasColumnName("concept").HasMaxLength(200).HasComment("Descripción del plan.");
            entity.Property(cf => cf.CurrencyCode).HasColumnName("currencyCode").HasMaxLength(3).IsFixedLength().HasComment("Moneda del financiamiento.");
            entity.Property(cf => cf.InitialBalance).HasColumnName("initialBalance").HasPrecision(18, 2).HasComment("Saldo inicial del plan.");
            entity.Property(cf => cf.OutstandingBalance).HasColumnName("outstandingBalance").HasPrecision(18, 2).HasComment("Saldo faltante a la fecha del corte.");
            entity.Property(cf => cf.Installments).HasColumnName("installments").HasMaxLength(20).HasComment("Cuotas en formato texto, ej. 3/12.");
            entity.Property(cf => cf.InstallmentAmount).HasColumnName("installmentAmount").HasPrecision(18, 2).HasComment("Monto de cada cuota.");
            entity.Property(cf => cf.TermMonths).HasColumnName("termMonths").HasComment("Plazo total en meses.");
            entity.Property(cf => cf.AnnualInterestRate).HasColumnName("annualInterestRate").HasPrecision(8, 4).HasComment("Tasa de interés anual; null si tasa cero.");
            entity.Property(cf => cf.DueDate).HasColumnName("dueDate").HasComment("Fecha de vencimiento del plan.");
            entity.Property(cf => cf.Status).HasColumnName("status").HasMaxLength(16).HasComment("Estado del financiamiento.");
            entity.Property(cf => cf.SourceFingerprint).HasColumnName("sourceFingerprint").HasMaxLength(64).IsFixedLength().HasComment("SHA-256 para deduplicación.");
            entity.Property(cf => cf.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(cf => cf.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
            entity.HasOne(cf => cf.BankAccount)
                .WithMany(a => a.CardFinancings)
                .HasForeignKey(cf => cf.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LoanStatement>(entity =>
        {
            entity.ToTable("tbLoanStatements", table =>
            {
                table.HasComment("Encabezado del extracto de préstamo. Padre de tbLoanPayments.");
                table.HasCheckConstraint("CK_tbLoanStatements_currencyCode", "[currencyCode] IN ('CRC', 'USD')");
            });
            entity.HasIndex(ls => new { ls.BankAccountId, ls.SourceFingerprint }).IsUnique();
            entity.Property(ls => ls.Id).HasColumnName("idLoanStatements").HasComment("Identificador único del extracto.");
            entity.Property(ls => ls.BankAccountId).HasColumnName("idBankAccounts").HasComment("Cuenta de préstamo asociada.");
            entity.Property(ls => ls.StatementDate).HasColumnName("statementDate").HasComment("Fecha del extracto.");
            entity.Property(ls => ls.CurrencyCode).HasColumnName("currencyCode").HasMaxLength(3).IsFixedLength().HasComment("Moneda del préstamo.");
            entity.Property(ls => ls.LoanNumber).HasColumnName("loanNumber").HasMaxLength(40).HasComment("Número de operación del préstamo.");
            entity.Property(ls => ls.OriginalLoanAmount).HasColumnName("originalLoanAmount").HasPrecision(18, 2).HasComment("Monto original de la deuda al momento de la formalización.");
            entity.Property(ls => ls.InterestRate).HasColumnName("interestRate").HasPrecision(8, 4).HasComment("Tasa de interés anual del préstamo.");
            entity.Property(ls => ls.TermMonths).HasColumnName("termMonths").HasComment("Plazo total en meses.");
            entity.Property(ls => ls.StartDate).HasColumnName("startDate").HasComment("Fecha de inicio o formalización del préstamo.");
            entity.Property(ls => ls.MaturityDate).HasColumnName("maturityDate").HasComment("Fecha de vencimiento del préstamo.");
            entity.Property(ls => ls.OutstandingBalance).HasColumnName("outstandingBalance").HasPrecision(18, 2).HasComment("Saldo pendiente total.");
            entity.Property(ls => ls.NextMonthCapital).HasColumnName("nextMonthCapital").HasPrecision(18, 2).HasComment("Capital de la próxima cuota vigente.");
            entity.Property(ls => ls.NextMonthInterest).HasColumnName("nextMonthInterest").HasPrecision(18, 2).HasComment("Interés de la próxima cuota vigente.");
            entity.Property(ls => ls.NextMonthTotal).HasColumnName("nextMonthTotal").HasPrecision(18, 2).HasComment("Total de la próxima cuota vigente.");
            entity.Property(ls => ls.CurrentPortionCapital).HasColumnName("currentPortionCapital").HasPrecision(18, 2).HasComment("Capital porción corriente (≤12 meses).");
            entity.Property(ls => ls.CurrentPortionInterest).HasColumnName("currentPortionInterest").HasPrecision(18, 2).HasComment("Interés porción corriente (≤12 meses).");
            entity.Property(ls => ls.CurrentPortionTotal).HasColumnName("currentPortionTotal").HasPrecision(18, 2).HasComment("Total porción corriente (≤12 meses).");
            entity.Property(ls => ls.LongTermCapital).HasColumnName("longTermCapital").HasPrecision(18, 2).HasComment("Capital largo plazo (>12 meses).");
            entity.Property(ls => ls.LongTermInterest).HasColumnName("longTermInterest").HasPrecision(18, 2).HasComment("Interés largo plazo (>12 meses).");
            entity.Property(ls => ls.LongTermTotal).HasColumnName("longTermTotal").HasPrecision(18, 2).HasComment("Total largo plazo (>12 meses).");
            entity.Property(ls => ls.SourceFingerprint).HasColumnName("sourceFingerprint").HasMaxLength(64).IsFixedLength().HasComment("SHA-256 para deduplicación.");
            entity.Property(ls => ls.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(ls => ls.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
            entity.HasOne(ls => ls.BankAccount)
                .WithMany(a => a.LoanStatements)
                .HasForeignKey(ls => ls.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LoanPayment>(entity =>
        {
            entity.ToTable("tbLoanPayments", table => table.HasComment("Cuotas del calendario de amortización de un préstamo."));
            entity.HasIndex(lp => new { lp.LoanStatementId, lp.SourceFingerprint }).IsUnique();
            entity.Property(lp => lp.Id).HasColumnName("idLoanPayments").HasComment("Identificador único de la cuota.");
            entity.Property(lp => lp.LoanStatementId).HasColumnName("idLoanStatements").HasComment("Extracto padre al que pertenece la cuota.");
            entity.Property(lp => lp.PaymentDate).HasColumnName("paymentDate").HasComment("Fecha de la cuota.");
            entity.Property(lp => lp.Capital).HasColumnName("capital").HasPrecision(18, 2).HasComment("Abono a capital.");
            entity.Property(lp => lp.Interest).HasColumnName("interest").HasPrecision(18, 2).HasComment("Interés de la cuota.");
            entity.Property(lp => lp.LateFee).HasColumnName("lateFee").HasPrecision(18, 2).HasComment("Mora.");
            entity.Property(lp => lp.OtherCharges).HasColumnName("otherCharges").HasPrecision(18, 2).HasComment("Otros cargos.");
            entity.Property(lp => lp.Total).HasColumnName("total").HasPrecision(18, 2).HasComment("Total de la cuota.");
            entity.Property(lp => lp.Balance).HasColumnName("balance").HasPrecision(18, 2).HasComment("Saldo después del pago.");
            entity.Property(lp => lp.SourceFingerprint).HasColumnName("sourceFingerprint").HasMaxLength(64).IsFixedLength().HasComment("SHA-256 para deduplicación.");
            entity.Property(lp => lp.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.HasOne(lp => lp.LoanStatement)
                .WithMany(ls => ls.Payments)
                .HasForeignKey(lp => lp.LoanStatementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedBanks(builder);
        SeedBankAccounts(builder);
        SeedExchangeRates(builder);
        SeedImportTemplates(builder);
        SeedBankAccountImportTemplates(builder);
        SeedImportTemplatePatterns(builder);
        SeedPeriods(builder);
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
        builder.Entity<ImportTemplate>().HasData(ImportTemplateCatalog.SeedTemplates());
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
        builder.Entity<ImportTemplatePattern>().HasData(ImportTemplateCatalog.SeedPatterns());
    }

    private static void SeedPeriods(ModelBuilder builder)
    {
        var createdAt = new DateTimeOffset(2026, 7, 21, 0, 0, 0, TimeSpan.FromHours(-6));
        var months = new[]
        {
            ("ENE-2026", new DateOnly(2025, 12, 19), new DateOnly(2026,  1, 18)),
            ("FEB-2026", new DateOnly(2026,  1, 19), new DateOnly(2026,  2, 18)),
            ("MAR-2026", new DateOnly(2026,  2, 19), new DateOnly(2026,  3, 18)),
            ("ABR-2026", new DateOnly(2026,  3, 19), new DateOnly(2026,  4, 18)),
            ("MAY-2026", new DateOnly(2026,  4, 19), new DateOnly(2026,  5, 18)),
            ("JUN-2026", new DateOnly(2026,  5, 19), new DateOnly(2026,  6, 18)),
            ("JUL-2026", new DateOnly(2026,  6, 19), new DateOnly(2026,  7, 18)),
            ("AGO-2026", new DateOnly(2026,  7, 19), new DateOnly(2026,  8, 18)),
            ("SEP-2026", new DateOnly(2026,  8, 19), new DateOnly(2026,  9, 18)),
            ("OCT-2026", new DateOnly(2026,  9, 19), new DateOnly(2026, 10, 18)),
            ("NOV-2026", new DateOnly(2026, 10, 19), new DateOnly(2026, 11, 18)),
            ("DIC-2026", new DateOnly(2026, 11, 19), new DateOnly(2026, 12, 18)),
        };

        builder.Entity<Period>().HasData(months.Select((m, i) => new Period
        {
            Id = Guid.Parse($"60000000-0000-0000-0000-{i + 1:D12}"),
            Label = m.Item1,
            StartDate = m.Item2,
            EndDate = m.Item3,
            CreatedAt = createdAt
        }));
    }
}
