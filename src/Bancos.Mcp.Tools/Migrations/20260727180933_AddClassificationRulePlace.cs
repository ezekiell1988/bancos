using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Mcp.Migrations
{
    /// <inheritdoc />
    public partial class AddClassificationRulePlace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "place",
                table: "tbClassificationRules",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                comment: "Lugar o comercio conocido que se aplica a coincidencias de la regla.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "place",
                table: "tbClassificationRules");
        }
    }
}
