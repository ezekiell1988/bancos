using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditFinancingCurrencyCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "CreditFinancings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "CreditFinancings");
        }
    }
}
