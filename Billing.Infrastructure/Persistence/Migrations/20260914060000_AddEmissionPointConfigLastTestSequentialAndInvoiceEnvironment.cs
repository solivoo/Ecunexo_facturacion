using System;
using Ecunexo.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(BillingDbContext))]
    [Migration("20260914060000_AddEmissionPointConfigLastTestSequentialAndInvoiceEnvironment")]
    public class AddEmissionPointConfigLastTestSequentialAndInvoiceEnvironment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastTestSequential",
                schema: "billing",
                table: "emission_point_configs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                schema: "billing",
                table: "electronic_invoices",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Production");

            migrationBuilder.DropIndex(
                name: "IX_electronic_invoices_EmitterId_Establishment_EmissionPoint_DocumentType_Sequential",
                schema: "billing",
                table: "electronic_invoices");

            migrationBuilder.CreateIndex(
                name: "IX_electronic_invoices_EmitterId_Environment_Establishment_EmissionPoint_DocumentType_Sequential",
                schema: "billing",
                table: "electronic_invoices",
                columns: new[] { "EmitterId", "Environment", "Establishment", "EmissionPoint", "DocumentType", "Sequential" },
                unique: true);

            migrationBuilder.Sql("""UPDATE billing.electronic_invoices SET "Environment" = 'Test' WHERE substring("AccessKey" from 24 for 1) = '1';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_electronic_invoices_EmitterId_Environment_Establishment_EmissionPoint_DocumentType_Sequential",
                schema: "billing",
                table: "electronic_invoices");

            migrationBuilder.CreateIndex(
                name: "IX_electronic_invoices_EmitterId_Establishment_EmissionPoint_DocumentType_Sequential",
                schema: "billing",
                table: "electronic_invoices",
                columns: new[] { "EmitterId", "Establishment", "EmissionPoint", "DocumentType", "Sequential" },
                unique: true);

            migrationBuilder.DropColumn(
                name: "Environment",
                schema: "billing",
                table: "electronic_invoices");

            migrationBuilder.DropColumn(
                name: "LastTestSequential",
                schema: "billing",
                table: "emission_point_configs");
        }
    }
}
