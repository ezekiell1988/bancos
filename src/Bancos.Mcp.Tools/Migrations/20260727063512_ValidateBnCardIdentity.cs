using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Mcp.Migrations
{
    /// <inheritdoc />
    public partial class ValidateBnCardIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbBankAccounts_identifierHash",
                table: "tbBankAccounts");

            migrationBuilder.UpdateData(
                table: "tbBankAccounts",
                keyColumn: "idBankAccounts",
                keyValue: new Guid("40000000-0000-0000-0000-000000000009"),
                columns: new[] { "cardFingerprint", "identifierHash" },
                values: new object[] { "C3F2ECAC42C4D0E8A3C87B37CE1047CA8F1AB81F19D2E2401D2EC549BE369B8D", "F81224881C588E934588730A5E60191CEF644390A8B0C728C3C33E1200A3DFB4" });

            migrationBuilder.UpdateData(
                table: "tbBankAccounts",
                keyColumn: "idBankAccounts",
                keyValue: new Guid("40000000-0000-0000-0000-000000000010"),
                columns: new[] { "cardFingerprint", "identifierHash" },
                values: new object[] { "C3F2ECAC42C4D0E8A3C87B37CE1047CA8F1AB81F19D2E2401D2EC549BE369B8D", "F81224881C588E934588730A5E60191CEF644390A8B0C728C3C33E1200A3DFB4" });

            migrationBuilder.CreateIndex(
                name: "IX_tbBankAccounts_identifierHash_currencyCode",
                table: "tbBankAccounts",
                columns: new[] { "identifierHash", "currencyCode" },
                unique: true,
                filter: "[identifierHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbBankAccounts_identifierHash_currencyCode",
                table: "tbBankAccounts");

            migrationBuilder.UpdateData(
                table: "tbBankAccounts",
                keyColumn: "idBankAccounts",
                keyValue: new Guid("40000000-0000-0000-0000-000000000009"),
                columns: new[] { "cardFingerprint", "identifierHash" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "tbBankAccounts",
                keyColumn: "idBankAccounts",
                keyValue: new Guid("40000000-0000-0000-0000-000000000010"),
                columns: new[] { "cardFingerprint", "identifierHash" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_tbBankAccounts_identifierHash",
                table: "tbBankAccounts",
                column: "identifierHash",
                unique: true,
                filter: "[identifierHash] IS NOT NULL");
        }
    }
}
