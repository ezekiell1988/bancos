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
    public DbSet<AccountPeriodClosing> AccountPeriodClosings => Set<AccountPeriodClosing>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ClassificationRule> ClassificationRules => Set<ClassificationRule>();
    public DbSet<TransactionClassification> TransactionClassifications => Set<TransactionClassification>();

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
            entity.HasIndex(account => new { account.IdentifierHash, account.CurrencyCode })
                .IsUnique()
                .HasFilter("[identifierHash] IS NOT NULL");
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
            entity.ToTable("tbLoanPayments", table =>
            {
                table.HasComment("Cuotas del calendario de amortización de un préstamo.");
                table.HasCheckConstraint("CK_tbLoanPayments_installmentNumber", "[installmentNumber] > 0");
                table.HasCheckConstraint("CK_tbLoanPayments_status", "[status] IN ('Pagada', 'Vigente')");
            });
            entity.HasIndex(lp => new { lp.LoanStatementId, lp.InstallmentNumber }).IsUnique();
            entity.HasIndex(lp => lp.InstallmentNumber);
            entity.Property(lp => lp.Id).HasColumnName("idLoanPayments").HasComment("Identificador único de la cuota.");
            entity.Property(lp => lp.LoanStatementId).HasColumnName("idLoanStatements").HasComment("Extracto padre al que pertenece la cuota.");
            entity.Property(lp => lp.InstallmentNumber).HasColumnName("installmentNumber").HasComment("Número consecutivo de cuota dentro del préstamo.");
            entity.Property(lp => lp.PaymentDate).HasColumnName("paymentDate").HasComment("Fecha de la cuota.");
            entity.Property(lp => lp.Capital).HasColumnName("capital").HasPrecision(18, 2).HasComment("Abono a capital.");
            entity.Property(lp => lp.Interest).HasColumnName("interest").HasPrecision(18, 2).HasComment("Interés de la cuota.");
            entity.Property(lp => lp.LateFee).HasColumnName("lateFee").HasPrecision(18, 2).HasComment("Mora.");
            entity.Property(lp => lp.OtherCharges).HasColumnName("otherCharges").HasPrecision(18, 2).HasComment("Otros cargos.");
            entity.Property(lp => lp.Total).HasColumnName("total").HasPrecision(18, 2).HasComment("Total de la cuota.");
            entity.Property(lp => lp.Balance).HasColumnName("balance").HasPrecision(18, 2).HasComment("Saldo después del pago.");
            entity.Property(lp => lp.Status).HasColumnName("status").HasMaxLength(16).HasComment("Estado actual de la cuota en el extracto.");
            entity.Property(lp => lp.SourceFingerprint).HasColumnName("sourceFingerprint").HasMaxLength(64).IsFixedLength().HasComment("SHA-256 para deduplicación.");
            entity.Property(lp => lp.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(lp => lp.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
            entity.HasOne(lp => lp.LoanStatement)
                .WithMany(ls => ls.Payments)
                .HasForeignKey(lp => lp.LoanStatementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AccountPeriodClosing>(entity =>
        {
            entity.ToTable("tbAccountPeriodClosings", table => table.HasComment("Saldo acumulado al cierre de cada periodo por cuenta bancaria."));
            entity.HasIndex(c => new { c.BankAccountId, c.PeriodId }).IsUnique();
            entity.Property(c => c.Id).HasColumnName("idAccountPeriodClosings").HasComment("Identificador único del cierre.");
            entity.Property(c => c.BankAccountId).HasColumnName("idBankAccounts").HasComment("Cuenta bancaria del cierre.");
            entity.Property(c => c.PeriodId).HasColumnName("idPeriods").HasComment("Período de reporte del cierre.");
            entity.Property(c => c.Balance).HasColumnName("balance").HasPrecision(18, 2).HasComment("Saldo acumulado al cierre del período en CRC.");
            entity.Property(c => c.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(c => c.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
            entity.HasOne(c => c.BankAccount)
                .WithMany()
                .HasForeignKey(c => c.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Period)
                .WithMany()
                .HasForeignKey(c => c.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Category>(entity =>
        {
            entity.ToTable("tbCategories", table =>
            {
                table.HasComment("Árbol de categorías contables usado por la clasificación determinista.");
                table.HasCheckConstraint("CK_tbCategories_rootType", "[rootType] IN ('income', 'expense', 'asset', 'liability', 'equity')");
            });
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.Id).HasColumnName("idCategories").HasComment("Identificador único de la categoría.");
            entity.Property(c => c.ParentId).HasColumnName("idParentCategories").HasComment("Categoría padre; null si es una raíz del árbol.");
            entity.Property(c => c.RootType).HasColumnName("rootType").HasMaxLength(16).HasComment("Raíz contable de la categoría: ingreso, gasto, activo, pasivo o capital.");
            entity.Property(c => c.Code).HasColumnName("code").HasMaxLength(80).HasComment("Código estable que identifica la categoría, ej. expense.groceries.");
            entity.Property(c => c.Name).HasColumnName("name").HasMaxLength(120).HasComment("Nombre visible de la categoría.");
            entity.Property(c => c.IsEnabled).HasColumnName("isEnabled").HasComment("Indica si la categoría puede usarse para clasificar movimientos.");
            entity.Property(c => c.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(c => c.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClassificationRule>(entity =>
        {
            entity.ToTable("tbClassificationRules", table =>
            {
                table.HasComment("Reglas deterministas para clasificar movimientos por cuenta, descripción y contexto.");
                table.HasCheckConstraint("CK_tbClassificationRules_matchType", "[matchType] IN ('exact', 'contains', 'starts-with')");
            });
            entity.HasIndex(r => new { r.BankAccountId, r.DescriptionPattern, r.MatchType, r.OperationType }).IsUnique();
            entity.Property(r => r.Id).HasColumnName("idClassificationRules").HasComment("Identificador único de la regla.");
            entity.Property(r => r.CategoryId).HasColumnName("idCategories").HasComment("Categoría asignada cuando la regla coincide.");
            entity.Property(r => r.BankAccountId).HasColumnName("idBankAccounts").HasComment("Cuenta bancaria a la que aplica la regla; null aplica a cualquier cuenta.");
            entity.Property(r => r.DescriptionPattern).HasColumnName("descriptionPattern").HasMaxLength(200).HasComment("Texto normalizado a comparar contra la descripción del movimiento.");
            entity.Property(r => r.MatchType).HasColumnName("matchType").HasMaxLength(16).HasComment("Forma de comparar el patrón: exacto, contiene o empieza con.");
            entity.Property(r => r.OperationType).HasColumnName("operationType").HasMaxLength(32).HasComment("Tipo de operación requerido; null aplica a cualquier tipo.");
            entity.Property(r => r.Priority).HasColumnName("priority").HasComment("Prioridad para desempatar entre reglas igual de específicas; mayor gana.");
            entity.Property(r => r.IsEnabled).HasColumnName("isEnabled").HasComment("Indica si la regla participa en la clasificación automática.");
            entity.Property(r => r.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora de creación del registro.");
            entity.Property(r => r.UpdatedAt).HasColumnName("updatedAt").HasComment("Fecha y hora de la última actualización del registro.");
            entity.HasOne(r => r.Category)
                .WithMany(c => c.Rules)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.BankAccount)
                .WithMany(a => a.ClassificationRules)
                .HasForeignKey(r => r.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TransactionClassification>(entity =>
        {
            entity.ToTable("tbTransactionClassifications", table =>
            {
                table.HasComment("Historial auditable de cada decisión de clasificación tomada sobre un movimiento.");
                table.HasCheckConstraint("CK_tbTransactionClassifications_source", "[source] IN ('rule', 'ai', 'manual', 'unclassified')");
                table.HasCheckConstraint("CK_tbTransactionClassifications_confidence", "[confidence] IS NULL OR ([confidence] >= 0 AND [confidence] <= 1)");
            });
            entity.HasIndex(tc => tc.TransactionId);
            entity.Property(tc => tc.Id).HasColumnName("idTransactionClassifications").HasComment("Identificador único de la entrada de clasificación.");
            entity.Property(tc => tc.TransactionId).HasColumnName("idTransactions").HasComment("Movimiento clasificado.");
            entity.Property(tc => tc.CategoryId).HasColumnName("idCategories").HasComment("Categoría asignada; null si el movimiento quedó sin clasificar.");
            entity.Property(tc => tc.ClassificationRuleId).HasColumnName("idClassificationRules").HasComment("Regla que produjo la clasificación; null si el origen no fue una regla.");
            entity.Property(tc => tc.Source).HasColumnName("source").HasMaxLength(16).HasComment("Origen de la decisión: regla, IA, manual o sin clasificar.");
            entity.Property(tc => tc.Confidence).HasColumnName("confidence").HasPrecision(5, 4).HasComment("Confianza de la decisión entre 0 y 1; null si no aplica.");
            entity.Property(tc => tc.Explanation).HasColumnName("explanation").HasMaxLength(500).HasComment("Explicación breve de por qué se asignó la categoría.");
            entity.Property(tc => tc.CreatedAt).HasColumnName("createdAt").HasComment("Fecha y hora en que se tomó la decisión.");
            entity.HasOne(tc => tc.Transaction)
                .WithMany(t => t.Classifications)
                .HasForeignKey(tc => tc.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(tc => tc.Category)
                .WithMany(c => c.Classifications)
                .HasForeignKey(tc => tc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(tc => tc.ClassificationRule)
                .WithMany(r => r.Classifications)
                .HasForeignKey(tc => tc.ClassificationRuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedInitialBalanceTransactions(builder);
        SeedBanks(builder);
        SeedBankAccounts(builder);
        SeedExchangeRates(builder);
        SeedImportTemplates(builder);
        SeedBankAccountImportTemplates(builder);
        SeedImportTemplatePatterns(builder);
        SeedPeriods(builder);
        SeedCategories(builder);
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
            Account(1, bac, "bac-credit-01-crc", "credit-card", "CRC", "07825F50C4920FED32C232E0AFADBAFB12EEB0762C5B99477CE80DC9CE0764F7"),
            Account(2, bac, "bac-credit-01-usd", "credit-card", "USD", "CB71E5C9AF2BE78C6045E02929B89590A28538EBA15491133CF1CE69FE4A6B29"),
            Account(3, bac, "bac-credit-02-crc", "credit-card", "CRC", "55A4EC76E34349CD6A33908B598B209DD0807AA10ACD3612EF058994C0FD684C"),
            Account(4, bac, "bac-credit-02-usd", "credit-card", "USD", "56A7FBE1229ADB5852D0099B0FEFC7F26327637D9E3AFF1609EFF3FB5AB06091"),
            Account(5, bac, "bac-credit-03-crc", "credit-card", "CRC", "F299DFDEB48DF802DDEDA26856F1A483739482932C307878EF65D88254FACA59"),
            Account(6, bac, "bac-credit-03-usd", "credit-card", "USD", "36A6585E5645DC484F13517E01A170767CAD3C6F82496E1B5F0206468603A4E0"),
            Account(7, bac, "bac-credit-04-crc", "credit-card", "CRC", "D0B207A37C18F6A3CC1367ECED0222D222B1232AACDCBAC8379B66DB5FCA5CC3"),
            Account(8, bac, "bac-credit-04-usd", "credit-card", "USD", "8E9F3E66F0952B0FEDD9A9CB2AB7C395811496C2FB948A1562FEC479A3C15A24"),
            Account(
                9, bn, "bn-credit-01-crc", "credit-card", "CRC",
                identifierHash: "F81224881C588E934588730A5E60191CEF644390A8B0C728C3C33E1200A3DFB4",
                cardFingerprint: "C3F2ECAC42C4D0E8A3C87B37CE1047CA8F1AB81F19D2E2401D2EC549BE369B8D"),
            Account(
                10, bn, "bn-credit-01-usd", "credit-card", "USD",
                identifierHash: "F81224881C588E934588730A5E60191CEF644390A8B0C728C3C33E1200A3DFB4",
                cardFingerprint: "C3F2ECAC42C4D0E8A3C87B37CE1047CA8F1AB81F19D2E2401D2EC549BE369B8D"),
            Account(11, bn, "bn-debit-01-usd", "debit-card", "USD", "BBAD6EA77F349D1265C4082AA7EBCD93D8F975221BE18D17720FA22027E06BA1"),
            Account(12, bcr, "bcr-debit-01-crc", "debit-card", "CRC", "A9A76820A1F0CEEA89995562122687A33EC9F971692DC4121E2A8D35CDE6343B"),
            Account(13, bac, "bac-debit-01-crc", "debit-card", "CRC", "DAFC04C14315C23B1207A1D2CD70B60839F2821BFE2A245938FAB6F863AA9DB5"),
            Account(14, bn, "bn-debit-01-crc", "debit-card", "CRC", "46995612194255ABF847233C41563CAFAA3EE6C77F5CBACF59DDA57F8BA34AAF"),
            Account(15, coopealianza, "coopealianza-loan-01-crc", "loan", "CRC"));

        BankAccount Account(
            int accountNumber,
            Guid bankId,
            string code,
            string accountType,
            string currencyCode,
            string? identifierHash = null,
            string? cardFingerprint = null) => new()
        {
            Id = Guid.Parse($"40000000-0000-0000-0000-{accountNumber:D12}"),
            BankId = bankId,
            Code = code,
            AccountType = accountType,
            CurrencyCode = currencyCode,
            IdentifierHash = identifierHash,
            CardFingerprint = cardFingerprint,
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
        var bnDebitUsd = Guid.Parse("40000000-0000-0000-0000-000000000011");
        var bnDebitCrc = Guid.Parse("40000000-0000-0000-0000-000000000014");
        var bacDebit = Guid.Parse("40000000-0000-0000-0000-000000000013");
        var coopealianzaLoan = Guid.Parse("40000000-0000-0000-0000-000000000015");

        var links = new List<BankAccountImportTemplate>();
        links.AddRange(Links([bcrDebit], 1, 3));
        links.AddRange(Links(bacCreditAccounts, 2, 5, 6, 8));
        links.AddRange(Links(bnCreditAccounts, 9));
        links.AddRange(Links([bnDebitUsd], 10, 4));  // BN USD: sentinel csv-v1 + XLS fallback
        links.AddRange(Links([bnDebitCrc], 11));      // BN CRC: sentinel csv-crc-v1
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

    private static void SeedInitialBalanceTransactions(ModelBuilder builder)
    {
        // Saldos iniciales: período anterior al primer movimiento importado por cuenta.
        // Fuente: columna "Previous balance" de los CSV de JUN-2026. Signo negativo = pasivo (deuda).
        // CR69 y CR48 tienen movs desde mayo → saldo inicial en ABR-2026 (fin Apr 18).
        // CR64 tiene movs desde JUN → saldo inicial en MAY-2026 (fin May 18).
        // CR63 tiene saldo anterior 0 → sin seed.
        var createdAt = new DateTimeOffset(2026, 7, 26, 0, 0, 0, TimeSpan.FromHours(-6));
        var periodEne2026 = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var periodAbr2026 = Guid.Parse("60000000-0000-0000-0000-000000000004");
        var periodMay2026 = Guid.Parse("60000000-0000-0000-0000-000000000005");
        var dateEne = new DateOnly(2025, 12, 19);
        var dateAbr = new DateOnly(2026, 4, 18);
        var dateMay = new DateOnly(2026, 5, 18);

        builder.Entity<Transaction>().HasData(
            // bac-credit-01-crc: saldo previo 177,724.97 CRC — primer mov 04/05 en MAY-2026 → seed en ABR-2026
            new Transaction
            {
                Id = Guid.Parse("A0000000-0000-0000-0000-000000000001"),
                BankAccountId = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                PeriodId = periodAbr2026,
                TransactionDate = dateAbr,
                Description = "Saldo inicial",
                CurrencyCode = "CRC",
                Amount = -177724.97m,
                AmountCrc = -177724.97m,
                ExchangeRate = 1m,
                OperationType = "other-charge",
                SourceFingerprint = "53414c494e4943494f310000000000000000000000000000000000000000001",
                CreatedAt = createdAt,
            },
            // bac-credit-02-crc: saldo previo 477,326.20 CRC — primer mov 13/05 en MAY-2026 → seed en ABR-2026
            new Transaction
            {
                Id = Guid.Parse("A0000000-0000-0000-0000-000000000003"),
                BankAccountId = Guid.Parse("40000000-0000-0000-0000-000000000003"),
                PeriodId = periodAbr2026,
                TransactionDate = dateAbr,
                Description = "Saldo inicial",
                CurrencyCode = "CRC",
                Amount = -477326.20m,
                AmountCrc = -477326.20m,
                ExchangeRate = 1m,
                OperationType = "other-charge",
                SourceFingerprint = "53414c494e4943494f330000000000000000000000000000000000000000003",
                CreatedAt = createdAt,
            },
            // bac-credit-02-usd: saldo previo 153.18 USD → 70,156.44 CRC @ 458 — primer mov 13/05 en MAY-2026 → seed en ABR-2026
            new Transaction
            {
                Id = Guid.Parse("A0000000-0000-0000-0000-000000000004"),
                BankAccountId = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                PeriodId = periodAbr2026,
                TransactionDate = dateAbr,
                Description = "Saldo inicial",
                CurrencyCode = "USD",
                Amount = -153.18m,
                AmountCrc = -70156.44m,
                ExchangeRate = 458m,
                OperationType = "other-charge",
                SourceFingerprint = "53414c494e4943494f340000000000000000000000000000000000000000004",
                CreatedAt = createdAt,
            },
            // bac-credit-03-crc: saldo previo 119,014.25 CRC (CR64, 3777-13**) — primer mov 19/05 en JUN-2026 → seed en MAY-2026
            new Transaction
            {
                Id = Guid.Parse("A0000000-0000-0000-0000-000000000005"),
                BankAccountId = Guid.Parse("40000000-0000-0000-0000-000000000005"),
                PeriodId = periodMay2026,
                TransactionDate = dateMay,
                Description = "Saldo inicial",
                CurrencyCode = "CRC",
                Amount = -119014.25m,
                AmountCrc = -119014.25m,
                ExchangeRate = 1m,
                OperationType = "other-charge",
                SourceFingerprint = "53414c494e4943494f350000000000000000000000000000000000000000005",
                CreatedAt = createdAt,
            },
            // bac-debit-01-crc: saldo inicial 10,551.04 CRC — del campo "Saldo Inicial" del XLS — primer mov 02/01/2026 en ENE-2026
            new Transaction
            {
                Id = Guid.Parse("A0000000-0000-0000-0000-000000000013"),
                BankAccountId = Guid.Parse("40000000-0000-0000-0000-000000000013"),
                PeriodId = periodEne2026,
                TransactionDate = dateEne,
                Description = "Saldo inicial",
                CurrencyCode = "CRC",
                Amount = 10551.04m,
                AmountCrc = 10551.04m,
                ExchangeRate = 1m,
                OperationType = "other-charge",
                SourceFingerprint = "53414c494e4943494f313300000000000000000000000000000000000000013",
                CreatedAt = createdAt,
            }
        );
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

    private static void SeedCategories(ModelBuilder builder)
    {
        var createdAt = new DateTimeOffset(2026, 7, 27, 0, 0, 0, TimeSpan.FromHours(-6));

        Guid CategoryId(int number) => Guid.Parse($"70000000-0000-0000-0000-{number:D12}");

        Category Root(int number, string rootType, string code, string name) => new()
        {
            Id = CategoryId(number),
            RootType = rootType,
            Code = code,
            Name = name,
            CreatedAt = createdAt
        };

        Category Child(int number, int parentNumber, string rootType, string code, string name) => new()
        {
            Id = CategoryId(number),
            ParentId = CategoryId(parentNumber),
            RootType = rootType,
            Code = code,
            Name = name,
            CreatedAt = createdAt
        };

        builder.Entity<Category>().HasData(
            Root(1, "income", "income", "Ingreso"),
            Root(2, "expense", "expense", "Gasto"),
            Root(3, "asset", "asset", "Activo"),
            Root(4, "liability", "liability", "Pasivo"),
            Root(5, "equity", "equity", "Capital"),
            Child(6, 1, "income", "income.salary", "Salario"),
            Child(7, 1, "income", "income.other", "Otros ingresos"),
            Child(8, 2, "expense", "expense.groceries", "Alimentación"),
            Child(9, 2, "expense", "expense.transport", "Transporte"),
            Child(10, 2, "expense", "expense.housing", "Vivienda"),
            Child(11, 2, "expense", "expense.utilities", "Servicios"),
            Child(12, 2, "expense", "expense.health", "Salud"),
            Child(13, 2, "expense", "expense.entertainment", "Entretenimiento"),
            Child(14, 2, "expense", "expense.other", "Otros gastos"),
            Child(15, 3, "asset", "asset.cash", "Efectivo y bancos"),
            Child(16, 4, "liability", "liability.creditCard", "Tarjetas de crédito"),
            Child(17, 4, "liability", "liability.loan", "Préstamos"));
    }
}
