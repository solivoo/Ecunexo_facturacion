using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistInvoicesEmittersOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "emitters",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Ruc = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TradeName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MainAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CertSerialNumber = table.Column<string>(type: "text", nullable: true),
                    CertNotBefore = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CertNotAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CertProvider = table.Column<string>(type: "text", nullable: true),
                    CertSecretName = table.Column<string>(type: "text", nullable: true),
                    CertLocation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emitters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sri_outbox",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmitterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Environment = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AccessKey = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sri_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "electronic_invoices",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmitterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmitterRuc = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    Establishment = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    EmissionPoint = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Sequential = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AccessKey = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: true),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CounterpartyJson = table.Column<string>(type: "jsonb", nullable: false),
                    SubtotalWithoutTax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotalsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SriTransmissionState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SriMessagesJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_electronic_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_electronic_invoices_emitters_EmitterId",
                        column: x => x.EmitterId,
                        principalSchema: "billing",
                        principalTable: "emitters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "establishments",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmitterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_establishments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_establishments_emitters_EmitterId",
                        column: x => x.EmitterId,
                        principalSchema: "billing",
                        principalTable: "emitters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotalWithoutTax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxesJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_lines_electronic_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "billing",
                        principalTable: "electronic_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_xml_artifacts",
                schema: "billing",
                columns: table => new
                {
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnsignedXml = table.Column<byte[]>(type: "bytea", nullable: true),
                    SignedXml = table.Column<byte[]>(type: "bytea", nullable: true),
                    AuthorizedXml = table.Column<string>(type: "text", nullable: true),
                    AuthorizationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_xml_artifacts", x => x.InvoiceId);
                    table.ForeignKey(
                        name: "FK_invoice_xml_artifacts_electronic_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "billing",
                        principalTable: "electronic_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emission_point_configs",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmissionPoint = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    LastSequential = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emission_point_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_emission_point_configs_establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalSchema: "billing",
                        principalTable: "establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_electronic_invoices_AccessKey",
                schema: "billing",
                table: "electronic_invoices",
                column: "AccessKey");

            migrationBuilder.CreateIndex(
                name: "IX_electronic_invoices_EmitterId_Establishment_EmissionPoint_D~",
                schema: "billing",
                table: "electronic_invoices",
                columns: new[] { "EmitterId", "Establishment", "EmissionPoint", "DocumentType", "Sequential" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_electronic_invoices_EmitterId_IssueDate",
                schema: "billing",
                table: "electronic_invoices",
                columns: new[] { "EmitterId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_emission_point_configs_EstablishmentId_EmissionPoint_Docume~",
                schema: "billing",
                table: "emission_point_configs",
                columns: new[] { "EstablishmentId", "EmissionPoint", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emitters_Ruc",
                schema: "billing",
                table: "emitters",
                column: "Ruc");

            migrationBuilder.CreateIndex(
                name: "IX_emitters_TenantId",
                schema: "billing",
                table: "emitters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_establishments_EmitterId_Code",
                schema: "billing",
                table: "establishments",
                columns: new[] { "EmitterId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_InvoiceId_LineNumber",
                schema: "billing",
                table: "invoice_lines",
                columns: new[] { "InvoiceId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sri_outbox_InvoiceId",
                schema: "billing",
                table: "sri_outbox",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_sri_outbox_Status_NextAttemptAt",
                schema: "billing",
                table: "sri_outbox",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emission_point_configs",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoice_lines",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoice_xml_artifacts",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "sri_outbox",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "establishments",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "electronic_invoices",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "emitters",
                schema: "billing");
        }
    }
}
