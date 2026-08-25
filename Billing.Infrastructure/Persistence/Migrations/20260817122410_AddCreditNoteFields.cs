using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditNoteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModifiedDocumentNumber",
                schema: "billing",
                table: "electronic_invoices",
                type: "character varying(17)",
                maxLength: 17,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedDocumentType",
                schema: "billing",
                table: "electronic_invoices",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedInvoiceId",
                schema: "billing",
                table: "electronic_invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ModifiedIssueDate",
                schema: "billing",
                table: "electronic_invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Motivo",
                schema: "billing",
                table: "electronic_invoices",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_electronic_invoices_ModifiedInvoiceId",
                schema: "billing",
                table: "electronic_invoices",
                column: "ModifiedInvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_electronic_invoices_ModifiedInvoiceId",
                schema: "billing",
                table: "electronic_invoices");

            migrationBuilder.DropColumn(
                name: "ModifiedDocumentNumber",
                schema: "billing",
                table: "electronic_invoices");

            migrationBuilder.DropColumn(
                name: "ModifiedDocumentType",
                schema: "billing",
                table: "electronic_invoices");

            migrationBuilder.DropColumn(
                name: "ModifiedInvoiceId",
                schema: "billing",
                table: "electronic_invoices");

            migrationBuilder.DropColumn(
                name: "ModifiedIssueDate",
                schema: "billing",
                table: "electronic_invoices");

            migrationBuilder.DropColumn(
                name: "Motivo",
                schema: "billing",
                table: "electronic_invoices");
        }
    }
}
