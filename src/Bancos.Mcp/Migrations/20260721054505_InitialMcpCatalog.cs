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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbBankAccountImportTemplates");

            migrationBuilder.DropTable(
                name: "tbExchangeRates");

            migrationBuilder.DropTable(
                name: "tbImportTemplatePatterns");

            migrationBuilder.DropTable(
                name: "tbBankAccounts");

            migrationBuilder.DropTable(
                name: "tbImportTemplates");

            migrationBuilder.DropTable(
                name: "tbBanks");
        }
    }
}
