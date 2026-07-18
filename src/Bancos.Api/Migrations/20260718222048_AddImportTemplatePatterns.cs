using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddImportTemplatePatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportTemplatePatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignatureHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ContentKind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Template = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportTemplatePatterns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportTemplatePatterns_SignatureHash",
                table: "ImportTemplatePatterns",
                column: "SignatureHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportTemplatePatterns");
        }
    }
}
