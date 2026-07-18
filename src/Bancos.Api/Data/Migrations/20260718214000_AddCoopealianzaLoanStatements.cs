using Bancos.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Api.Data.Migrations;

[DbContext(typeof(BancosDbContext))]
[Migration("20260718214000_AddCoopealianzaLoanStatements")]
public partial class AddCoopealianzaLoanStatements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LoanStatements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AccountAuxiliaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                SourceFingerprint = table.Column<string>(type: "nvarchar(450)", nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoanStatements", x => x.Id);
                table.ForeignKey("FK_LoanStatements_AccountAuxiliaries_AccountAuxiliaryId", x => x.AccountAuxiliaryId, "AccountAuxiliaries", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_LoanStatements_Imports_ImportId", x => x.ImportId, "Imports", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "LoanPayments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LoanStatementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                Capital = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Interest = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                LateFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                OtherCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                SourceFingerprint = table.Column<string>(type: "nvarchar(450)", nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoanPayments", x => x.Id);
                table.ForeignKey("FK_LoanPayments_LoanStatements_LoanStatementId", x => x.LoanStatementId, "LoanStatements", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_LoanStatements_AccountAuxiliaryId_SourceFingerprint", "LoanStatements", new[] { "AccountAuxiliaryId", "SourceFingerprint" }, unique: true);
        migrationBuilder.CreateIndex("IX_LoanStatements_ImportId", "LoanStatements", "ImportId");
        migrationBuilder.CreateIndex("IX_LoanPayments_LoanStatementId_SourceFingerprint", "LoanPayments", new[] { "LoanStatementId", "SourceFingerprint" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LoanPayments");
        migrationBuilder.DropTable(name: "LoanStatements");
    }
}
