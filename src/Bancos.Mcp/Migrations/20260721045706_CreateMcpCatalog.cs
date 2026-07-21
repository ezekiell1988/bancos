using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bancos.Mcp.Migrations
{
    /// <inheritdoc />
    public partial class CreateMcpCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbImportTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ContentKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ParserKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbImportTemplates", x => x.Id);
                    table.CheckConstraint("CK_tbImportTemplates_ContentKind", "[ContentKind] IN ('csv', 'html', 'xls', 'pdf')");
                });

            migrationBuilder.CreateTable(
                name: "tbImportTemplatePatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignatureHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: true),
                    PatternKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequiredTermsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlternativeTermGroupsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetectorVersion = table.Column<short>(type: "smallint", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbImportTemplatePatterns", x => x.Id);
                    table.CheckConstraint("CK_tbImportTemplatePatterns_Definition", "[SignatureHash] IS NOT NULL OR [RequiredTermsJson] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_tbImportTemplatePatterns_tbImportTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "tbImportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "tbImportTemplates",
                columns: new[] { "Id", "Code", "ContentKind", "CreatedAt", "IsEnabled", "Name", "ParserKey", "UpdatedAt" },
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
                table: "tbImportTemplatePatterns",
                columns: new[] { "Id", "AlternativeTermGroupsJson", "CreatedAt", "DetectorVersion", "IsActive", "IsApproved", "PatternKind", "RequiredTermsJson", "SignatureHash", "TemplateId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, true, true, "content-terms", "[\";\",\"oficina\",\"fechamovimiento\",\"numerodocumento\",\"debito\",\"credito\",\"descripcion\"]", null, new Guid("10000000-0000-0000-0000-000000000001"), null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "[[\"dollar amount\",\"dollars amount\"]]", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, true, true, "content-terms", "[\",\",\"name\",\"date\",\"minimum payment\",\"cash payment\",\"local amount\"]", null, new Guid("10000000-0000-0000-0000-000000000002"), null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "[[\"movimientos por rango de fechas\",\"movimientos de la cuenta\",\"movimientos del d\"]]", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, true, true, "content-terms", "[\"banco de costa rica\"]", null, new Guid("10000000-0000-0000-0000-000000000003"), null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "[[\"descripcion\",\"detalle\"],[\"debito\",\"debitos\"],[\"credito\",\"creditos\"]]", new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, true, true, "content-terms", "[\"fecha\"]", null, new Guid("10000000-0000-0000-0000-000000000004"), null },
                    { new Guid("20000000-0000-0000-0000-000000000005"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, true, true, "content-terms", "[\"consulta de financiamientos\",\"fecha\",\"concepto\",\"cuotas\",\"monto de cuota\",\"saldo inicial\",\"saldo faltante\"]", null, new Guid("10000000-0000-0000-0000-000000000005"), null },
                    { new Guid("20000000-0000-0000-0000-000000000006"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, true, true, "content-terms", "[\"tarjeta de credito\",\"saldo en colones\",\"saldo en dolares\",\"fecha de pago de contado\"]", null, new Guid("10000000-0000-0000-0000-000000000006"), null },
                    { new Guid("20000000-0000-0000-0000-000000000007"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, true, true, "content-terms", "[\"ver detalles del prestamo\",\"capital\",\"interes\",\"mora\",\"otros\",\"total\",\"saldo\"]", null, new Guid("10000000-0000-0000-0000-000000000007"), null },
                    { new Guid("20000000-0000-0000-0000-000000000008"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, true, true, "content-terms", "[\"numero de tarjeta\",\"marca de tarjeta\",\"plan de lealtad\",\"pagos vencidos\",\"pago de contado\",\"fecha de corte\",\"total pago de contado\"]", null, new Guid("10000000-0000-0000-0000-000000000008"), null },
                    { new Guid("20000000-0000-0000-0000-000000000009"), null, new DateTimeOffset(new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), (short)1, true, true, "content-terms", "[\"banco nacional de costa rica\",\"estado de cuenta tarjetas de credito\",\"detalle de compras del periodo\",\"total pago de contado\"]", null, new Guid("10000000-0000-0000-0000-000000000009"), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbImportTemplatePatterns_SignatureHash",
                table: "tbImportTemplatePatterns",
                column: "SignatureHash",
                unique: true,
                filter: "[SignatureHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbImportTemplatePatterns_TemplateId",
                table: "tbImportTemplatePatterns",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_tbImportTemplates_Code",
                table: "tbImportTemplates",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbImportTemplatePatterns");

            migrationBuilder.DropTable(
                name: "tbImportTemplates");
        }
    }
}
