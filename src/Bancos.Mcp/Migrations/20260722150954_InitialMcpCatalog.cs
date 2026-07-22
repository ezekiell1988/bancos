using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bancos.Mcp.Migrations
{
    /// <inheritdoc />
    public partial class InitialMcpCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbBanks",
                columns: table => new
                {
                    idBanks = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único del banco."),
                    code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Código corto que identifica al banco."),
                    name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false, comment: "Nombre comercial o legal del banco."),
                    isEnabled = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si el banco puede usarse en el catálogo."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbBanks", x => x.idBanks);
                },
                comment: "Catálogo de entidades bancarias disponibles para cuentas y tipos de cambio.");

            migrationBuilder.CreateTable(
                name: "tbImportTemplates",
                columns: table => new
                {
                    idImportTemplates = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único de la plantilla de importación."),
                    code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, comment: "Código estable que identifica la plantilla."),
                    name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false, comment: "Nombre descriptivo de la plantilla de importación."),
                    contentKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Tipo de contenido esperado en el archivo."),
                    parserKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, comment: "Clave del analizador que procesa el formato."),
                    isEnabled = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si la plantilla puede utilizarse para detectar archivos."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbImportTemplates", x => x.idImportTemplates);
                    table.CheckConstraint("CK_tbImportTemplates_contentKind", "[contentKind] IN ('csv', 'html', 'xls', 'pdf')");
                },
                comment: "Catálogo de formatos de archivos de importación reconocidos.");

            migrationBuilder.CreateTable(
                name: "tbPeriods",
                columns: table => new
                {
                    idPeriods = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único del período."),
                    label = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "Nombre visible del período, ej. JUL-2026."),
                    startDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de inicio del período (día 19 del mes anterior)."),
                    endDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de cierre del período (día 18 del mes en curso)."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbPeriods", x => x.idPeriods);
                },
                comment: "Períodos de reporte financiero. Cada período corre del 19 de un mes al 18 del siguiente.");

            migrationBuilder.CreateTable(
                name: "tbBankAccounts",
                columns: table => new
                {
                    idBankAccounts = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único de la cuenta bancaria."),
                    idBanks = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador del banco propietario de la cuenta."),
                    code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, comment: "Código interno no sensible que identifica la cuenta."),
                    identifierHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true, comment: "Huella criptográfica opcional del identificador bancario normalizado."),
                    cardFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true, comment: "Huella criptográfica opcional de la tarjeta asociada."),
                    accountType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Tipo de producto financiero: tarjeta de crédito, débito o préstamo."),
                    currencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false, comment: "Código de moneda permitido para la cuenta."),
                    isEnabled = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si la cuenta puede usarse en el catálogo."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbBankAccounts", x => x.idBankAccounts);
                    table.CheckConstraint("CK_tbBankAccounts_accountType", "[accountType] IN ('credit-card', 'debit-card', 'loan')");
                    table.CheckConstraint("CK_tbBankAccounts_currencyCode", "[currencyCode] IN ('CRC', 'USD')");
                    table.ForeignKey(
                        name: "FK_tbBankAccounts_tbBanks_idBanks",
                        column: x => x.idBanks,
                        principalTable: "tbBanks",
                        principalColumn: "idBanks",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catálogo de cuentas, tarjetas y préstamos asociados a un banco.");

            migrationBuilder.CreateTable(
                name: "tbExchangeRates",
                columns: table => new
                {
                    idExchangeRates = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único del tipo de cambio."),
                    idBanks = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador del banco que publica el tipo de cambio."),
                    rateDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de vigencia del tipo de cambio."),
                    currencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false, comment: "Moneda cotizada; actualmente solo se permite USD."),
                    crcPerUnit = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "Cantidad de colones costarricenses equivalente a una unidad de moneda."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbExchangeRates", x => x.idExchangeRates);
                    table.CheckConstraint("CK_tbExchangeRates_crcPerUnit", "[crcPerUnit] > 0");
                    table.CheckConstraint("CK_tbExchangeRates_currencyCode", "[currencyCode] = 'USD'");
                    table.ForeignKey(
                        name: "FK_tbExchangeRates_tbBanks_idBanks",
                        column: x => x.idBanks,
                        principalTable: "tbBanks",
                        principalColumn: "idBanks",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Tipos de cambio de USD expresados en colones costarricenses por banco y fecha.");

            migrationBuilder.CreateTable(
                name: "tbImportTemplatePatterns",
                columns: table => new
                {
                    idImportTemplatePatterns = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único del patrón de detección."),
                    idImportTemplates = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador de la plantilla asociada al patrón."),
                    signatureHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true, comment: "Huella opcional del contenido que identifica el formato."),
                    patternKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "Tipo de patrón usado para la detección."),
                    requiredTermsJson = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "Términos que deben existir en el contenido para aceptar el patrón."),
                    alternativeTermGroupsJson = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "Grupos de términos alternativos aceptados por el patrón."),
                    detectorVersion = table.Column<short>(type: "smallint", nullable: false, comment: "Versión del algoritmo de detección asociado."),
                    isApproved = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si el patrón fue aprobado para uso productivo."),
                    isActive = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si el patrón está habilitado para detectar archivos."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbImportTemplatePatterns", x => x.idImportTemplatePatterns);
                    table.CheckConstraint("CK_tbImportTemplatePatterns_definition", "[signatureHash] IS NOT NULL OR [requiredTermsJson] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_tbImportTemplatePatterns_tbImportTemplates_idImportTemplates",
                        column: x => x.idImportTemplates,
                        principalTable: "tbImportTemplates",
                        principalColumn: "idImportTemplates",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Patrones aprobados para detectar una plantilla de importación por contenido.");

            migrationBuilder.CreateTable(
                name: "tbBankAccountImportTemplates",
                columns: table => new
                {
                    idBankAccounts = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador de la cuenta bancaria compatible con la plantilla."),
                    idImportTemplates = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador de la plantilla compatible con la cuenta.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbBankAccountImportTemplates", x => new { x.idBankAccounts, x.idImportTemplates });
                    table.ForeignKey(
                        name: "FK_tbBankAccountImportTemplates_tbBankAccounts_idBankAccounts",
                        column: x => x.idBankAccounts,
                        principalTable: "tbBankAccounts",
                        principalColumn: "idBankAccounts",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbBankAccountImportTemplates_tbImportTemplates_idImportTemplates",
                        column: x => x.idImportTemplates,
                        principalTable: "tbImportTemplates",
                        principalColumn: "idImportTemplates",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Relación entre cuentas bancarias y formatos de importación admitidos.");

            migrationBuilder.CreateTable(
                name: "tbCardFinancings",
                columns: table => new
                {
                    idCardFinancings = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único del financiamiento."),
                    idBankAccounts = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Tarjeta de crédito asociada."),
                    referenceNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true, comment: "Número de referencia del financiamiento."),
                    financingDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha del financiamiento."),
                    concept = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Descripción del plan."),
                    currencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false, comment: "Moneda del financiamiento."),
                    initialBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Saldo inicial del plan."),
                    outstandingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Saldo faltante a la fecha del corte."),
                    installments = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "Cuotas en formato texto, ej. 3/12."),
                    installmentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto de cada cuota."),
                    termMonths = table.Column<short>(type: "smallint", nullable: true, comment: "Plazo total en meses."),
                    annualInterestRate = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: true, comment: "Tasa de interés anual; null si tasa cero."),
                    dueDate = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha de vencimiento del plan."),
                    status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Estado del financiamiento."),
                    sourceFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, comment: "SHA-256 para deduplicación."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbCardFinancings", x => x.idCardFinancings);
                    table.CheckConstraint("CK_tbCardFinancings_currencyCode", "[currencyCode] IN ('CRC', 'USD')");
                    table.CheckConstraint("CK_tbCardFinancings_status", "[status] IN ('active', 'cancelled', 'settled')");
                    table.ForeignKey(
                        name: "FK_tbCardFinancings_tbBankAccounts_idBankAccounts",
                        column: x => x.idBankAccounts,
                        principalTable: "tbBankAccounts",
                        principalColumn: "idBankAccounts",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Planes de cuotas y financiamientos activos en tarjeta (snapshot del estado actual).");

            migrationBuilder.CreateTable(
                name: "tbCardStatements",
                columns: table => new
                {
                    idCardStatements = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único del corte."),
                    idBankAccounts = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Tarjeta de crédito asociada al corte."),
                    statementDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de corte."),
                    periodLabel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "Período informativo del header, ej. JUL-2026."),
                    minimumPaymentDueDate = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha límite pago mínimo."),
                    cashPaymentDueDate = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha límite pago de contado."),
                    previousBalanceCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    previousBalanceUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    purchasesTotalCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    purchasesTotalUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    paymentsTotalCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    paymentsTotalUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    interestTotalCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    interestTotalUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    currentBalanceCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    currentBalanceUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    minimumPaymentCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    minimumPaymentUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    cashPaymentCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    cashPaymentUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    creditLimitCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    creditLimitUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    availableBalanceCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    availableBalanceUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    sourceFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, comment: "SHA-256 para deduplicación."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbCardStatements", x => x.idCardStatements);
                    table.ForeignKey(
                        name: "FK_tbCardStatements_tbBankAccounts_idBankAccounts",
                        column: x => x.idBankAccounts,
                        principalTable: "tbBankAccounts",
                        principalColumn: "idBankAccounts",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Header del corte mensual de tarjeta de crédito con totales del período.");

            migrationBuilder.CreateTable(
                name: "tbLoanStatements",
                columns: table => new
                {
                    idLoanStatements = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único del extracto."),
                    idBankAccounts = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Cuenta de préstamo asociada."),
                    statementDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha del extracto."),
                    currencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false, comment: "Moneda del préstamo."),
                    loanNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true, comment: "Número de operación del préstamo."),
                    originalLoanAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true, comment: "Monto original de la deuda al momento de la formalización."),
                    interestRate = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: true, comment: "Tasa de interés anual del préstamo."),
                    termMonths = table.Column<int>(type: "int", nullable: true, comment: "Plazo total en meses."),
                    startDate = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha de inicio o formalización del préstamo."),
                    maturityDate = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha de vencimiento del préstamo."),
                    outstandingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Saldo pendiente total."),
                    sourceFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, comment: "SHA-256 para deduplicación."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbLoanStatements", x => x.idLoanStatements);
                    table.CheckConstraint("CK_tbLoanStatements_currencyCode", "[currencyCode] IN ('CRC', 'USD')");
                    table.ForeignKey(
                        name: "FK_tbLoanStatements_tbBankAccounts_idBankAccounts",
                        column: x => x.idBankAccounts,
                        principalTable: "tbBankAccounts",
                        principalColumn: "idBankAccounts",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Encabezado del extracto de préstamo. Padre de tbLoanPayments.");

            migrationBuilder.CreateTable(
                name: "tbTransactions",
                columns: table => new
                {
                    idTransactions = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único del movimiento."),
                    idBankAccounts = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Cuenta bancaria origen del movimiento."),
                    idPeriods = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Período de reporte; null si aún no se ha creado el período."),
                    referenceNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true, comment: "N. Referencia del extracto."),
                    transactionDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de la transacción."),
                    paymentDate = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fecha de pago, si aplica."),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Concepto o descripción del movimiento."),
                    place = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true, comment: "Lugar o comercio donde se realizó la transacción."),
                    currencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false, comment: "Moneda de la transacción."),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto original; positivo=cargo, negativo=abono."),
                    amountCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Monto convertido a colones."),
                    exchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true, comment: "Tipo de cambio usado para la conversión."),
                    operationType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "Tipo de operación."),
                    sourceFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, comment: "SHA-256 para deduplicación."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbTransactions", x => x.idTransactions);
                    table.CheckConstraint("CK_tbTransactions_currencyCode", "[currencyCode] IN ('CRC', 'USD')");
                    table.CheckConstraint("CK_tbTransactions_operationType", "[operationType] IN ('purchase', 'payment', 'interest', 'other-charge', 'interest-reversal')");
                    table.ForeignKey(
                        name: "FK_tbTransactions_tbBankAccounts_idBankAccounts",
                        column: x => x.idBankAccounts,
                        principalTable: "tbBankAccounts",
                        principalColumn: "idBankAccounts",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbTransactions_tbPeriods_idPeriods",
                        column: x => x.idPeriods,
                        principalTable: "tbPeriods",
                        principalColumn: "idPeriods",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Movimientos individuales extraídos de estados de cuenta.");

            migrationBuilder.CreateTable(
                name: "tbLoanPayments",
                columns: table => new
                {
                    idLoanPayments = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único de la cuota."),
                    idLoanStatements = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Extracto padre al que pertenece la cuota."),
                    paymentDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fecha de la cuota."),
                    capital = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Abono a capital."),
                    interest = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Interés de la cuota."),
                    lateFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Mora."),
                    otherCharges = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Otros cargos."),
                    total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Total de la cuota."),
                    balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Saldo después del pago."),
                    sourceFingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false, comment: "SHA-256 para deduplicación."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbLoanPayments", x => x.idLoanPayments);
                    table.ForeignKey(
                        name: "FK_tbLoanPayments_tbLoanStatements_idLoanStatements",
                        column: x => x.idLoanStatements,
                        principalTable: "tbLoanStatements",
                        principalColumn: "idLoanStatements",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Cuotas del calendario de amortización de un préstamo.");

            migrationBuilder.CreateTable(
                name: "tbCardStatementLines",
                columns: table => new
                {
                    idCardStatementLines = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único de la línea."),
                    idCardStatements = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Corte al que pertenece el movimiento."),
                    idTransactions = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Movimiento incluido en el corte."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbCardStatementLines", x => x.idCardStatementLines);
                    table.ForeignKey(
                        name: "FK_tbCardStatementLines_tbCardStatements_idCardStatements",
                        column: x => x.idCardStatements,
                        principalTable: "tbCardStatements",
                        principalColumn: "idCardStatements",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbCardStatementLines_tbTransactions_idTransactions",
                        column: x => x.idTransactions,
                        principalTable: "tbTransactions",
                        principalColumn: "idTransactions",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Auxiliar que asocia movimientos a un corte de tarjeta. Surrogate PK + UNIQUE constraint per ADR-03.");

            migrationBuilder.InsertData(
                table: "tbBanks",
                columns: new[] { "idBanks", "code", "createdAt", "isEnabled", "name", "updatedAt" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), "BCR", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Banco de Costa Rica", null },
                    { new Guid("30000000-0000-0000-0000-000000000002"), "BN", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Banco Nacional de Costa Rica", null },
                    { new Guid("30000000-0000-0000-0000-000000000003"), "BAC", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "BAC Credomatic", null },
                    { new Guid("30000000-0000-0000-0000-000000000004"), "COOPEALIANZA", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Coopealianza", null }
                });

            migrationBuilder.InsertData(
                table: "tbImportTemplates",
                columns: new[] { "idImportTemplates", "code", "contentKind", "createdAt", "isEnabled", "name", "parserKey", "updatedAt" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "bcr-debit-csv-v1", "csv", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Movimientos de cuenta BCR", "bcr-debit-csv", null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "bac-credit-csv-v1", "csv", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Resumen de tarjeta BAC", "bac-credit-csv", null },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "bcr-debit-html-xls-v1", "html", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Movimientos BCR HTML", "bcr-debit-html", null },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "bank-account-movements-xls-v1", "xls", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Movimientos de cuenta XLS", "bank-account-movements-xls", null },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "bac-credit-financing-xls-v1", "xls", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Financiamientos BAC", "bac-credit-financing-xls", null },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "bac-credit-online-pdf-v1", "pdf", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Tarjeta BAC en linea", "bac-credit-online-pdf", null },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "coopealianza-loan-pdf-v1", "pdf", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Prestamo Coopealianza", "coopealianza-loan-pdf", null },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "bac-account-statement-pdf-v1", "pdf", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Estado de cuenta consolidado BAC", "bac-account-statement-pdf", null },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "bn-card-statement-pdf-v1", "pdf", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Estado de cuenta de tarjeta Banco Nacional", "bn-card-statement-pdf", null }
                });

            migrationBuilder.InsertData(
                table: "tbPeriods",
                columns: new[] { "idPeriods", "createdAt", "endDate", "label", "startDate", "updatedAt" },
                values: new object[,]
                {
                    { new Guid("60000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 1, 18), "ENE-2026", new DateOnly(2025, 12, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 2, 18), "FEB-2026", new DateOnly(2026, 1, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 3, 18), "MAR-2026", new DateOnly(2026, 2, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 4, 18), "ABR-2026", new DateOnly(2026, 3, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 5, 18), "MAY-2026", new DateOnly(2026, 4, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 6, 18), "JUN-2026", new DateOnly(2026, 5, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 7, 18), "JUL-2026", new DateOnly(2026, 6, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 8, 18), "AGO-2026", new DateOnly(2026, 7, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000009"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 9, 18), "SEP-2026", new DateOnly(2026, 8, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 10, 18), "OCT-2026", new DateOnly(2026, 9, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000011"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 11, 18), "NOV-2026", new DateOnly(2026, 10, 19), null },
                    { new Guid("60000000-0000-0000-0000-000000000012"), new DateTimeOffset(new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), new DateOnly(2026, 12, 18), "DIC-2026", new DateOnly(2026, 11, 19), null }
                });

            migrationBuilder.InsertData(
                table: "tbBankAccounts",
                columns: new[] { "idBankAccounts", "accountType", "idBanks", "cardFingerprint", "code", "createdAt", "currencyCode", "identifierHash", "isEnabled", "updatedAt" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), "credit-card", new Guid("30000000-0000-0000-0000-000000000003"), null, "bac-credit-01-crc", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "CRC", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000002"), "credit-card", new Guid("30000000-0000-0000-0000-000000000003"), null, "bac-credit-01-usd", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "USD", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000003"), "credit-card", new Guid("30000000-0000-0000-0000-000000000003"), null, "bac-credit-02-crc", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "CRC", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000004"), "credit-card", new Guid("30000000-0000-0000-0000-000000000003"), null, "bac-credit-02-usd", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "USD", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000005"), "credit-card", new Guid("30000000-0000-0000-0000-000000000003"), null, "bac-credit-03-crc", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "CRC", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000006"), "credit-card", new Guid("30000000-0000-0000-0000-000000000003"), null, "bac-credit-03-usd", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "USD", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000007"), "credit-card", new Guid("30000000-0000-0000-0000-000000000003"), null, "bac-credit-04-crc", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "CRC", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000008"), "credit-card", new Guid("30000000-0000-0000-0000-000000000003"), null, "bac-credit-04-usd", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "USD", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000009"), "credit-card", new Guid("30000000-0000-0000-0000-000000000002"), null, "bn-credit-01-crc", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "CRC", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000010"), "credit-card", new Guid("30000000-0000-0000-0000-000000000002"), null, "bn-credit-01-usd", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "USD", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000011"), "debit-card", new Guid("30000000-0000-0000-0000-000000000002"), null, "bn-debit-01-usd", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "USD", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000012"), "debit-card", new Guid("30000000-0000-0000-0000-000000000001"), null, "bcr-debit-01-crc", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "CRC", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000013"), "debit-card", new Guid("30000000-0000-0000-0000-000000000003"), null, "bac-debit-01-crc", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "CRC", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000014"), "debit-card", new Guid("30000000-0000-0000-0000-000000000002"), null, "bn-debit-01-crc", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "CRC", null, true, null },
                    { new Guid("40000000-0000-0000-0000-000000000015"), "loan", new Guid("30000000-0000-0000-0000-000000000004"), null, "coopealianza-loan-01-crc", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "CRC", null, true, null }
                });

            migrationBuilder.InsertData(
                table: "tbExchangeRates",
                columns: new[] { "idExchangeRates", "idBanks", "crcPerUnit", "createdAt", "currencyCode", "rateDate" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000002"), 458m, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "USD", new DateOnly(2026, 7, 20) },
                    { new Guid("50000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000003"), 458m, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), "USD", new DateOnly(2026, 7, 20) }
                });

            migrationBuilder.InsertData(
                table: "tbImportTemplatePatterns",
                columns: new[] { "idImportTemplatePatterns", "alternativeTermGroupsJson", "createdAt", "detectorVersion", "idImportTemplates", "isActive", "isApproved", "patternKind", "requiredTermsJson", "signatureHash", "updatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, new Guid("10000000-0000-0000-0000-000000000001"), true, true, "content-terms", "[\";\",\"oficina\",\"fechamovimiento\",\"numerodocumento\",\"debito\",\"credito\",\"descripcion\"]", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "[[\"dollar amount\",\"dollars amount\"]]", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, new Guid("10000000-0000-0000-0000-000000000002"), true, true, "content-terms", "[\",\",\"name\",\"date\",\"minimum payment\",\"cash payment\",\"local amount\"]", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "[[\"movimientos por rango de fechas\",\"movimientos de la cuenta\",\"movimientos del d\"]]", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, new Guid("10000000-0000-0000-0000-000000000003"), true, true, "content-terms", "[\"banco de costa rica\"]", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "[[\"descripcion\",\"detalle\"],[\"debito\",\"debitos\"],[\"credito\",\"creditos\"]]", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, new Guid("10000000-0000-0000-0000-000000000004"), true, true, "content-terms", "[\"fecha\"]", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000005"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, new Guid("10000000-0000-0000-0000-000000000005"), true, true, "content-terms", "[\"consulta de financiamientos\",\"fecha\",\"concepto\",\"cuotas\",\"monto de cuota\",\"saldo inicial\",\"saldo faltante\"]", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000006"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, new Guid("10000000-0000-0000-0000-000000000006"), true, true, "content-terms", "[\"tarjeta de credito\",\"saldo en colones\",\"saldo en dolares\",\"fecha de pago de contado\"]", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000007"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, new Guid("10000000-0000-0000-0000-000000000007"), true, true, "content-terms", "[\"ver detalles del prestamo\",\"capital\",\"interes\",\"mora\",\"otros\",\"total\",\"saldo\"]", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000008"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, new Guid("10000000-0000-0000-0000-000000000008"), true, true, "content-terms", "[\"numero de tarjeta\",\"marca de tarjeta\",\"plan de lealtad\",\"pagos vencidos\",\"pago de contado\",\"fecha de corte\",\"total pago de contado\"]", null, null },
                    { new Guid("20000000-0000-0000-0000-000000000009"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, new Guid("10000000-0000-0000-0000-000000000009"), true, true, "content-terms", "[\"banco nacional de costa rica\",\"estado de cuenta tarjetas de credito\",\"detalle de compras del periodo\",\"total pago de contado\"]", null, null }
                });

            migrationBuilder.InsertData(
                table: "tbBankAccountImportTemplates",
                columns: new[] { "idBankAccounts", "idImportTemplates" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("40000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("40000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("40000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("40000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("40000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000005") },
                    { new Guid("40000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000006") },
                    { new Guid("40000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000008") },
                    { new Guid("40000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000009") },
                    { new Guid("40000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000009") },
                    { new Guid("40000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000004") },
                    { new Guid("40000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("40000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("40000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000004") },
                    { new Guid("40000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000004") },
                    { new Guid("40000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000007") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbBankAccountImportTemplates_idImportTemplates",
                table: "tbBankAccountImportTemplates",
                column: "idImportTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_tbBankAccounts_idBanks_code",
                table: "tbBankAccounts",
                columns: new[] { "idBanks", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbBankAccounts_identifierHash",
                table: "tbBankAccounts",
                column: "identifierHash",
                unique: true,
                filter: "[identifierHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbBanks_code",
                table: "tbBanks",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbCardFinancings_idBankAccounts_sourceFingerprint",
                table: "tbCardFinancings",
                columns: new[] { "idBankAccounts", "sourceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbCardStatementLines_idCardStatements_idTransactions",
                table: "tbCardStatementLines",
                columns: new[] { "idCardStatements", "idTransactions" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbCardStatementLines_idTransactions",
                table: "tbCardStatementLines",
                column: "idTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_tbCardStatements_idBankAccounts_statementDate",
                table: "tbCardStatements",
                columns: new[] { "idBankAccounts", "statementDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbExchangeRates_idBanks_rateDate_currencyCode",
                table: "tbExchangeRates",
                columns: new[] { "idBanks", "rateDate", "currencyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbImportTemplatePatterns_idImportTemplates",
                table: "tbImportTemplatePatterns",
                column: "idImportTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_tbImportTemplatePatterns_signatureHash",
                table: "tbImportTemplatePatterns",
                column: "signatureHash",
                unique: true,
                filter: "[signatureHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbImportTemplates_code",
                table: "tbImportTemplates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbLoanPayments_idLoanStatements_sourceFingerprint",
                table: "tbLoanPayments",
                columns: new[] { "idLoanStatements", "sourceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbLoanStatements_idBankAccounts_sourceFingerprint",
                table: "tbLoanStatements",
                columns: new[] { "idBankAccounts", "sourceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbPeriods_endDate",
                table: "tbPeriods",
                column: "endDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbPeriods_label",
                table: "tbPeriods",
                column: "label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbPeriods_startDate",
                table: "tbPeriods",
                column: "startDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbTransactions_idBankAccounts_sourceFingerprint",
                table: "tbTransactions",
                columns: new[] { "idBankAccounts", "sourceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbTransactions_idPeriods",
                table: "tbTransactions",
                column: "idPeriods");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbBankAccountImportTemplates");

            migrationBuilder.DropTable(
                name: "tbCardFinancings");

            migrationBuilder.DropTable(
                name: "tbCardStatementLines");

            migrationBuilder.DropTable(
                name: "tbExchangeRates");

            migrationBuilder.DropTable(
                name: "tbImportTemplatePatterns");

            migrationBuilder.DropTable(
                name: "tbLoanPayments");

            migrationBuilder.DropTable(
                name: "tbCardStatements");

            migrationBuilder.DropTable(
                name: "tbTransactions");

            migrationBuilder.DropTable(
                name: "tbImportTemplates");

            migrationBuilder.DropTable(
                name: "tbLoanStatements");

            migrationBuilder.DropTable(
                name: "tbPeriods");

            migrationBuilder.DropTable(
                name: "tbBankAccounts");

            migrationBuilder.DropTable(
                name: "tbBanks");
        }
    }
}
