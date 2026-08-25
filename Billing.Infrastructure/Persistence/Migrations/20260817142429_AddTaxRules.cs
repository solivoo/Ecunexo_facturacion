using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxRules",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRules_Code_ValidFrom",
                schema: "billing",
                table: "TaxRules",
                columns: new[] { "Code", "ValidFrom" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO billing."TaxRules" ("Id", "Code", "Description", "PayloadJson", "ValidFrom", "ValidTo")
                VALUES
                ('0198c0a0-0001-7000-8000-000000000001', 'sri.void.online',
                 'Anulación en línea SRI hasta el día 10 del mes siguiente.',
                 '{"deadlineDayOfFollowingMonth":10,"extendToNextWeekday":true,"consumerFinalCannotVoid":true,"consumerFinalCannotCreditNote":true}'::jsonb,
                 DATE '2000-01-01', DATE '2025-07-31'),
                ('0198c0a0-0001-7000-8000-000000000002', 'sri.void.online',
                 'Anulación en línea SRI hasta el día 7 del mes siguiente (NAC-DGERCGC25).',
                 '{"deadlineDayOfFollowingMonth":7,"extendToNextWeekday":true,"consumerFinalCannotVoid":true,"consumerFinalCannotCreditNote":true}'::jsonb,
                 DATE '2025-08-01', NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxRules",
                schema: "billing");
        }
    }
}
