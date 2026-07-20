using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedGeneralCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedUtc", "Name", "ParentId", "UpdatedUtc" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000301"), new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), "General", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000301"));
        }
    }
}
