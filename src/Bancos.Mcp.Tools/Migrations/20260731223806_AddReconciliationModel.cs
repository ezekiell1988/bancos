using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Mcp.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbReconciliations",
                columns: table => new
                {
                    idReconciliations = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único de la conciliación."),
                    status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Estado de la conciliación: propuesta, confirmada o eliminada."),
                    confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, comment: "Confianza determinista calculada entre cero y uno."),
                    explanation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, comment: "Explicación de montos, fechas y confianza de la propuesta."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de creación de la conciliación."),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de la última modificación."),
                    confirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "Fecha y hora de confirmación manual."),
                    confirmedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true, comment: "Actor que confirmó la conciliación.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbReconciliations", x => x.idReconciliations);
                    table.CheckConstraint("CK_tbReconciliations_confidence", "[confidence] >= 0 AND [confidence] <= 1");
                    table.CheckConstraint("CK_tbReconciliations_status", "[status] IN ('proposed', 'confirmed', 'deleted')");
                },
                comment: "Grupos auditables de partidas conciliadas entre pagos y transferencias.");

            migrationBuilder.CreateTable(
                name: "tbReconciliationAudits",
                columns: table => new
                {
                    idReconciliationAudits = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único del evento de auditoría."),
                    idReconciliations = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Conciliación afectada por el evento."),
                    action = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Acción registrada sobre la conciliación."),
                    actor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, comment: "Actor que ejecutó la acción."),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Motivo declarado para la acción."),
                    snapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "Estado anterior serializado para auditoría."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora del evento de auditoría.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbReconciliationAudits", x => x.idReconciliationAudits);
                    table.ForeignKey(
                        name: "FK_tbReconciliationAudits_tbReconciliations_idReconciliations",
                        column: x => x.idReconciliations,
                        principalTable: "tbReconciliations",
                        principalColumn: "idReconciliations",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Auditoría inmutable de propuestas, confirmaciones, correcciones y eliminaciones de conciliaciones.");

            migrationBuilder.CreateTable(
                name: "tbReconciliationItems",
                columns: table => new
                {
                    idReconciliationItems = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador único de la línea de conciliación."),
                    idReconciliations = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Conciliación a la que pertenece la partida."),
                    idTransactions = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Movimiento original asociado; nunca se elimina al conciliar."),
                    side = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Lado de la relación: pago o transferencia."),
                    amountCrc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Valor absoluto en CRC usado para comparar ambos lados."),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "Fecha y hora de asociación de la partida.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbReconciliationItems", x => x.idReconciliationItems);
                    table.CheckConstraint("CK_tbReconciliationItems_amountCrc", "[amountCrc] >= 0");
                    table.CheckConstraint("CK_tbReconciliationItems_side", "[side] IN ('payment', 'transfer')");
                    table.ForeignKey(
                        name: "FK_tbReconciliationItems_tbReconciliations_idReconciliations",
                        column: x => x.idReconciliations,
                        principalTable: "tbReconciliations",
                        principalColumn: "idReconciliations",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbReconciliationItems_tbTransactions_idTransactions",
                        column: x => x.idTransactions,
                        principalTable: "tbTransactions",
                        principalColumn: "idTransactions",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Partidas que forman cada conciliación N:N.");

            migrationBuilder.CreateIndex(
                name: "IX_tbReconciliationAudits_idReconciliations_createdAt",
                table: "tbReconciliationAudits",
                columns: new[] { "idReconciliations", "createdAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tbReconciliationItems_idReconciliations_idTransactions",
                table: "tbReconciliationItems",
                columns: new[] { "idReconciliations", "idTransactions" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbReconciliationItems_idTransactions",
                table: "tbReconciliationItems",
                column: "idTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_tbReconciliations_status",
                table: "tbReconciliations",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbReconciliationAudits");

            migrationBuilder.DropTable(
                name: "tbReconciliationItems");

            migrationBuilder.DropTable(
                name: "tbReconciliations");
        }
    }
}
