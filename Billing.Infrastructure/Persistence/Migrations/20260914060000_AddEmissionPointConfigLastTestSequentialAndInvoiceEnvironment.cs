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

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'billing' AND tablename = 'electronic_invoices' AND indexname = 'IX_electronic_invoices_EmitterId_Establishment_EmissionPoint_D~') THEN
                        DROP INDEX billing."IX_electronic_invoices_EmitterId_Establishment_EmissionPoint_D~";
                    ELSIF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'billing' AND tablename = 'electronic_invoices' AND indexname = 'IX_electronic_invoices_EmitterId_Establishment_EmissionPoint_DocumentType_Sequential') THEN
                        DROP INDEX billing."IX_electronic_invoices_EmitterId_Establishment_EmissionPoint_DocumentType_Sequential";
                    ELSIF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'billing' AND tablename = 'electronic_invoices' AND indexname LIKE 'IX_electronic_invoices_EmitterId_Establishment_EmissionPoint_%') THEN
                        EXECUTE (SELECT 'DROP INDEX billing.' || quote_ident(indexname) FROM pg_indexes WHERE schemaname = 'billing' AND tablename = 'electronic_invoices' AND indexname LIKE 'IX_electronic_invoices_EmitterId_Establishment_EmissionPoint_%' LIMIT 1);
                    END IF;
                END $$;
            """);

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
