using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancos.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportTemplateAndTransactionIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalReference",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ImportId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SourceFingerprint",
                table: "Transactions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountAuxiliaryId",
                table: "Imports",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Imports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Template",
                table: "Imports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountAuxiliaryId_SourceFingerprint",
                table: "Transactions",
                columns: new[] { "AccountAuxiliaryId", "SourceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ImportId",
                table: "Transactions",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_Imports_AccountAuxiliaryId",
                table: "Imports",
                column: "AccountAuxiliaryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Imports_AccountAuxiliaries_AccountAuxiliaryId",
                table: "Imports",
                column: "AccountAuxiliaryId",
                principalTable: "AccountAuxiliaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Imports_ImportId",
                table: "Transactions",
                column: "ImportId",
                principalTable: "Imports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Imports_AccountAuxiliaries_AccountAuxiliaryId",
                table: "Imports");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Imports_ImportId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountAuxiliaryId_SourceFingerprint",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ImportId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Imports_AccountAuxiliaryId",
                table: "Imports");

            migrationBuilder.DropColumn(
                name: "ExternalReference",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ImportId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SourceFingerprint",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AccountAuxiliaryId",
                table: "Imports");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Imports");

            migrationBuilder.DropColumn(
                name: "Template",
                table: "Imports");
        }
    }
}
