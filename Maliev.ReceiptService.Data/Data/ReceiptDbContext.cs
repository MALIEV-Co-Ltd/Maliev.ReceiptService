using Microsoft.EntityFrameworkCore;
using Maliev.Aspire.ServiceDefaults.Database;
using Maliev.ReceiptService.Data.Models.Entities;
using Maliev.ReceiptService.Data.Models.Enums;

namespace Maliev.ReceiptService.Data.Data;

public class ReceiptDbContext : DbContext
{
    public ReceiptDbContext(DbContextOptions<ReceiptDbContext> options) : base(options) { }

    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLineItem> ReceiptLineItems => Set<ReceiptLineItem>();
    public DbSet<ReceiptAuditEvent> ReceiptAuditEvents => Set<ReceiptAuditEvent>();
    public DbSet<InvoiceBalanceTracker> InvoiceBalanceTrackers => Set<InvoiceBalanceTracker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Receipt configuration
        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ReceiptNumber)
                .IsUnique();

            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.IssueDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TaxAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.WithholdingTaxAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .ValueGeneratedOnAddOrUpdate();

            // One-to-Many: Receipt -> ReceiptLineItems (cascade delete)
            entity.HasMany(e => e.LineItems)
                .WithOne(e => e.Receipt)
                .HasForeignKey(e => e.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: Receipt -> ReceiptAuditEvents (no cascade)
            entity.HasMany(e => e.AuditEvents)
                .WithOne(e => e.Receipt)
                .HasForeignKey(e => e.ReceiptId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ReceiptLineItem configuration
        modelBuilder.Entity<ReceiptLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ReceiptId);
            entity.HasIndex(e => e.InvoiceLineItemId);

            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18,4)");

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TaxRate)
                .HasColumnType("decimal(5,2)");

            entity.Property(e => e.LineTotal)
                .HasColumnType("decimal(18,2)");
        });

        // ReceiptAuditEvent configuration
        modelBuilder.Entity<ReceiptAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ReceiptId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.RetainUntil);
            entity.HasIndex(e => e.EventType);

            entity.Property(e => e.EventType)
                .HasConversion<string>();
        });

        // InvoiceBalanceTracker configuration (US4 - composite key for segment support)
        modelBuilder.Entity<InvoiceBalanceTracker>(entity =>
        {
            // Composite key: InvoiceId + SegmentId (Guid.Empty for whole-invoice tracking)
            entity.HasKey(e => new { e.InvoiceId, e.SegmentId });

            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.RemainingBalance);

            entity.Property(e => e.TotalInvoiceAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalReceiptedAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.RemainingBalance)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .ValueGeneratedOnAddOrUpdate();
        });

        // Apply PostgreSQL snake_case naming convention globally
        SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);
    }
}
