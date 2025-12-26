using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.ReceiptService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invoice_balance_trackers",
                columns: table => new
                {
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_invoice_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_receipted_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    remaining_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_balance_trackers", x => new { x.invoice_id, x.segment_id });
                });

            migrationBuilder.CreateTable(
                name: "receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_segment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tax_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    customer_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    withholding_tax_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    pdf_reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    voided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "receipt_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    staff_member_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    previous_state = table.Column<string>(type: "text", nullable: true),
                    new_state = table.Column<string>(type: "text", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retain_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_receipt_audit_events_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalTable: "receipts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "receipt_line_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_line_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt_line_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_receipt_line_items_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_balance_trackers_invoice_id",
                table: "invoice_balance_trackers",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_balance_trackers_remaining_balance",
                table: "invoice_balance_trackers",
                column: "remaining_balance");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_audit_events_event_type",
                table: "receipt_audit_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_audit_events_receipt_id",
                table: "receipt_audit_events",
                column: "receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_audit_events_retain_until",
                table: "receipt_audit_events",
                column: "retain_until");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_audit_events_timestamp",
                table: "receipt_audit_events",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_line_items_invoice_line_item_id",
                table: "receipt_line_items",
                column: "invoice_line_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_line_items_receipt_id",
                table: "receipt_line_items",
                column: "receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_receipts_created_at",
                table: "receipts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_receipts_invoice_id",
                table: "receipts",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_receipts_issue_date",
                table: "receipts",
                column: "issue_date");

            migrationBuilder.CreateIndex(
                name: "ix_receipts_receipt_number",
                table: "receipts",
                column: "receipt_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_receipts_status",
                table: "receipts",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_balance_trackers");

            migrationBuilder.DropTable(
                name: "receipt_audit_events");

            migrationBuilder.DropTable(
                name: "receipt_line_items");

            migrationBuilder.DropTable(
                name: "receipts");
        }
    }
}
