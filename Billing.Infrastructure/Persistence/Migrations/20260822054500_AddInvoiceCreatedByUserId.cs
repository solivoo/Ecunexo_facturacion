using System;
using Ecunexo.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(BillingDbContext))]
    [Migration("20260822054500_AddInvoiceCreatedByUserId")]
    public class AddInvoiceCreatedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                schema: "billing",
                table: "electronic_invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_electronic_invoices_EmitterId_CreatedByUserId",
                schema: "billing",
                table: "electronic_invoices",
                columns: new[] { "EmitterId", "CreatedByUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_electronic_invoices_EmitterId_CreatedByUserId",
                schema: "billing",
                table: "electronic_invoices");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "billing",
                table: "electronic_invoices");
        }
    }
}
