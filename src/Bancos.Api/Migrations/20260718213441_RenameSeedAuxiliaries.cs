using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameSeedAuxiliaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccountAuxiliaries",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000201"),
                column: "Name",
                value: "Cuenta bancaria");

            migrationBuilder.UpdateData(
                table: "AccountAuxiliaries",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000202"),
                column: "Name",
                value: "Créditos y financiamientos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccountAuxiliaries",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000201"),
                column: "Name",
                value: "Cuenta transaccional CRC");

            migrationBuilder.UpdateData(
                table: "AccountAuxiliaries",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000202"),
                column: "Name",
                value: "Financiamientos");
        }
    }
}
