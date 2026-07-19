using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.ReceiptService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptExternalPaymentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "external_payment_id",
                table: "receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_receipts_invoice_id_external_payment_id",
                table: "receipts",
                columns: new[] { "invoice_id", "external_payment_id" },
                unique: true,
                filter: "external_payment_id IS NOT NULL AND status <> 'Void'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_receipts_invoice_id_external_payment_id",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "external_payment_id",
                table: "receipts");
        }
    }
}
