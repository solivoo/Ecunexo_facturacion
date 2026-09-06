using Ecunexo.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(BillingDbContext))]
[Migration("20260817010000_AddInvoiceRideFields")]
public class AddInvoiceRideFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AdditionalNote",
            schema: "billing",
            table: "electronic_invoices",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PaymentTermDays",
            schema: "billing",
            table: "electronic_invoices",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "MainCode",
            schema: "billing",
            table: "invoice_lines",
            type: "character varying(25)",
            maxLength: 25,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AdditionalNote",
            schema: "billing",
            table: "electronic_invoices");

        migrationBuilder.DropColumn(
            name: "PaymentTermDays",
            schema: "billing",
            table: "electronic_invoices");

        migrationBuilder.DropColumn(
            name: "MainCode",
            schema: "billing",
            table: "invoice_lines");
    }
}
