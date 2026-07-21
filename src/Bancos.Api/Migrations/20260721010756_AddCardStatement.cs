using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCardStatement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountAuxiliaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardNumberMasked = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CardBrand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoyaltyPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MinimumPaymentCrc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinimumPaymentUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashPaymentCrc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashPaymentUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardStatements_AccountAuxiliaries_AccountAuxiliaryId",
                        column: x => x.AccountAuxiliaryId,
                        principalTable: "AccountAuxiliaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardStatements_Imports_ImportId",
                        column: x => x.ImportId,
                        principalTable: "Imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardStatements_AccountAuxiliaryId_CardNumberMasked_StatementDate",
                table: "CardStatements",
                columns: new[] { "AccountAuxiliaryId", "CardNumberMasked", "StatementDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardStatements_ImportId",
                table: "CardStatements",
                column: "ImportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardStatements");
        }
    }
}
