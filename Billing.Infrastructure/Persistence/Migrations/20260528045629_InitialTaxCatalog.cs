using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTaxCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "TaxRates",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    RateCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WithholdingRates",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxType = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    RetentionCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithholdingRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_TaxCode_RateCode_ValidFrom",
                schema: "billing",
                table: "TaxRates",
                columns: new[] { "TaxCode", "RateCode", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_WithholdingRates_TaxType_RetentionCode_ValidFrom",
                schema: "billing",
                table: "WithholdingRates",
                columns: new[] { "TaxType", "RetentionCode", "ValidFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxRates",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "WithholdingRates",
                schema: "billing");
        }
    }
}
