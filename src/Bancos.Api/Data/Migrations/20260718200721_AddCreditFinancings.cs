using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditFinancings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditFinancings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountAuxiliaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Concept = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Installments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InitialBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditFinancings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditFinancings_AccountAuxiliaries_AccountAuxiliaryId",
                        column: x => x.AccountAuxiliaryId,
                        principalTable: "AccountAuxiliaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditFinancings_Imports_ImportId",
                        column: x => x.ImportId,
                        principalTable: "Imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditFinancings_AccountAuxiliaryId_SourceFingerprint",
                table: "CreditFinancings",
                columns: new[] { "AccountAuxiliaryId", "SourceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditFinancings_ImportId",
                table: "CreditFinancings",
                column: "ImportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditFinancings");
        }
    }
}
