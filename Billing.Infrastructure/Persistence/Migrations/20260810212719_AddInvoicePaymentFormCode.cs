using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentFormCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentFormCode",
                schema: "billing",
                table: "electronic_invoices",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "01");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentFormCode",
                schema: "billing",
                table: "electronic_invoices");
        }
    }
}
