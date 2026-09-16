using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bancos.Mcp.Migrations
{
    /// <inheritdoc />
    public partial class AddClassificationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbCategories",
                columns: table => new
                {
                    idCategories = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único de la categoría."),
                    idParentCategories = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Categoría padre; null si es una raíz del árbol."),
                    rootType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Raíz contable de la categoría: ingreso, gasto, activo, pasivo o capital."),
                    code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, comment: "Código estable que identifica la categoría, ej. expense.groceries."),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, comment: "Nombre visible de la categoría."),
                    isEnabled = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si la categoría puede usarse para clasificar movimientos."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbCategories", x => x.idCategories);
                    table.CheckConstraint("CK_tbCategories_rootType", "[rootType] IN ('income', 'expense', 'asset', 'liability', 'equity')");
                    table.ForeignKey(
                        name: "FK_tbCategories_tbCategories_idParentCategories",
                        column: x => x.idParentCategories,
                        principalTable: "tbCategories",
                        principalColumn: "idCategories",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Árbol de categorías contables usado por la clasificación determinista.");

            migrationBuilder.CreateTable(
                name: "tbClassificationRules",
                columns: table => new
                {
                    idClassificationRules = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único de la regla."),
                    idCategories = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Categoría asignada cuando la regla coincide."),
                    idBankAccounts = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Cuenta bancaria a la que aplica la regla; null aplica a cualquier cuenta."),
                    descriptionPattern = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Texto normalizado a comparar contra la descripción del movimiento."),
                    matchType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Forma de comparar el patrón: exacto, contiene o empieza con."),
                    operationType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "Tipo de operación requerido; null aplica a cualquier tipo."),
                    priority = table.Column<int>(type: "int", nullable: false, comment: "Prioridad para desempatar entre reglas igual de específicas; mayor gana."),
                    isEnabled = table.Column<bool>(type: "bit", nullable: false, comment: "Indica si la regla participa en la clasificación automática."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación del registro."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última actualización del registro.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbClassificationRules", x => x.idClassificationRules);
                    table.CheckConstraint("CK_tbClassificationRules_matchType", "[matchType] IN ('exact', 'contains', 'starts-with')");
                    table.ForeignKey(
                        name: "FK_tbClassificationRules_tbBankAccounts_idBankAccounts",
                        column: x => x.idBankAccounts,
                        principalTable: "tbBankAccounts",
                        principalColumn: "idBankAccounts",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbClassificationRules_tbCategories_idCategories",
                        column: x => x.idCategories,
                        principalTable: "tbCategories",
                        principalColumn: "idCategories",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Reglas deterministas para clasificar movimientos por cuenta, descripción y contexto.");

            migrationBuilder.CreateTable(
                name: "tbTransactionClassifications",
                columns: table => new
                {
                    idTransactionClassifications = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único de la entrada de clasificación."),
                    idTransactions = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Movimiento clasificado."),
                    idCategories = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Categoría asignada; null si el movimiento quedó sin clasificar."),
                    idClassificationRules = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Regla que produjo la clasificación; null si el origen no fue una regla."),
                    source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Origen de la decisión: regla, IA, manual o sin clasificar."),
                    confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true, comment: "Confianza de la decisión entre 0 y 1; null si no aplica."),
                    explanation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Explicación breve de por qué se asignó la categoría."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora en que se tomó la decisión.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbTransactionClassifications", x => x.idTransactionClassifications);
                    table.CheckConstraint("CK_tbTransactionClassifications_confidence", "[confidence] IS NULL OR ([confidence] >= 0 AND [confidence] <= 1)");
                    table.CheckConstraint("CK_tbTransactionClassifications_source", "[source] IN ('rule', 'ai', 'manual', 'unclassified')");
                    table.ForeignKey(
                        name: "FK_tbTransactionClassifications_tbCategories_idCategories",
                        column: x => x.idCategories,
                        principalTable: "tbCategories",
                        principalColumn: "idCategories",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbTransactionClassifications_tbClassificationRules_idClassificationRules",
                        column: x => x.idClassificationRules,
                        principalTable: "tbClassificationRules",
                        principalColumn: "idClassificationRules",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbTransactionClassifications_tbTransactions_idTransactions",
                        column: x => x.idTransactions,
                        principalTable: "tbTransactions",
                        principalColumn: "idTransactions",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Historial auditable de cada decisión de clasificación tomada sobre un movimiento.");

            migrationBuilder.InsertData(
                table: "tbCategories",
                columns: new[] { "idCategories", "code", "createdAt", "isEnabled", "name", "idParentCategories", "rootType", "updatedAt" },
                values: new object[,]
                {
                    { new Guid("70000000-0000-0000-0000-000000000001"), "income", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Ingreso", null, "income", null },
                    { new Guid("70000000-0000-0000-0000-000000000002"), "expense", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Gasto", null, "expense", null },
                    { new Guid("70000000-0000-0000-0000-000000000003"), "asset", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Activo", null, "asset", null },
                    { new Guid("70000000-0000-0000-0000-000000000004"), "liability", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Pasivo", null, "liability", null },
                    { new Guid("70000000-0000-0000-0000-000000000005"), "equity", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Capital", null, "equity", null },
                    { new Guid("70000000-0000-0000-0000-000000000006"), "income.salary", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Salario", new Guid("70000000-0000-0000-0000-000000000001"), "income", null },
                    { new Guid("70000000-0000-0000-0000-000000000007"), "income.other", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Otros ingresos", new Guid("70000000-0000-0000-0000-000000000001"), "income", null },
                    { new Guid("70000000-0000-0000-0000-000000000008"), "expense.groceries", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Alimentación", new Guid("70000000-0000-0000-0000-000000000002"), "expense", null },
                    { new Guid("70000000-0000-0000-0000-000000000009"), "expense.transport", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Transporte", new Guid("70000000-0000-0000-0000-000000000002"), "expense", null },
                    { new Guid("70000000-0000-0000-0000-000000000010"), "expense.housing", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Vivienda", new Guid("70000000-0000-0000-0000-000000000002"), "expense", null },
                    { new Guid("70000000-0000-0000-0000-000000000011"), "expense.utilities", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Servicios", new Guid("70000000-0000-0000-0000-000000000002"), "expense", null },
                    { new Guid("70000000-0000-0000-0000-000000000012"), "expense.health", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Salud", new Guid("70000000-0000-0000-0000-000000000002"), "expense", null },
                    { new Guid("70000000-0000-0000-0000-000000000013"), "expense.entertainment", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Entretenimiento", new Guid("70000000-0000-0000-0000-000000000002"), "expense", null },
                    { new Guid("70000000-0000-0000-0000-000000000014"), "expense.other", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Otros gastos", new Guid("70000000-0000-0000-0000-000000000002"), "expense", null },
                    { new Guid("70000000-0000-0000-0000-000000000015"), "asset.cash", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Efectivo y bancos", new Guid("70000000-0000-0000-0000-000000000003"), "asset", null },
                    { new Guid("70000000-0000-0000-0000-000000000016"), "liability.creditCard", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Tarjetas de crédito", new Guid("70000000-0000-0000-0000-000000000004"), "liability", null },
                    { new Guid("70000000-0000-0000-0000-000000000017"), "liability.loan", new DateTimeOffset(new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, -6, 0, 0, 0)), true, "Préstamos", new Guid("70000000-0000-0000-0000-000000000004"), "liability", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbCategories_code",
                table: "tbCategories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbCategories_idParentCategories",
                table: "tbCategories",
                column: "idParentCategories");

            migrationBuilder.CreateIndex(
                name: "IX_tbClassificationRules_idBankAccounts_descriptionPattern_matchType_operationType",
                table: "tbClassificationRules",
                columns: new[] { "idBankAccounts", "descriptionPattern", "matchType", "operationType" },
                unique: true,
                filter: "[idBankAccounts] IS NOT NULL AND [operationType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbClassificationRules_idCategories",
                table: "tbClassificationRules",
                column: "idCategories");

            migrationBuilder.CreateIndex(
                name: "IX_tbTransactionClassifications_idCategories",
                table: "tbTransactionClassifications",
                column: "idCategories");

            migrationBuilder.CreateIndex(
                name: "IX_tbTransactionClassifications_idClassificationRules",
                table: "tbTransactionClassifications",
                column: "idClassificationRules");

            migrationBuilder.CreateIndex(
                name: "IX_tbTransactionClassifications_idTransactions",
                table: "tbTransactionClassifications",
                column: "idTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbTransactionClassifications");

            migrationBuilder.DropTable(
                name: "tbClassificationRules");

            migrationBuilder.DropTable(
                name: "tbCategories");
        }
    }
}
