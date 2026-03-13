using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.ReceiptService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddXminConcurrencyAndReceiptSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "receipt_number_seq");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "receipts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "receipt_line_items",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "receipt_audit_events",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "invoice_balance_trackers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "receipt_line_items");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "receipt_audit_events");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "invoice_balance_trackers");

            migrationBuilder.DropSequence(
                name: "receipt_number_seq");
        }
    }
}
