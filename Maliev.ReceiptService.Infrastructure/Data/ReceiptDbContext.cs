using Maliev.Aspire.ServiceDefaults.Database;
using Maliev.ReceiptService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.ReceiptService.Infrastructure.Data;

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

        // MassTransit transactional outbox entities
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ReceiptNumber)
                .IsUnique();

            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => new { e.InvoiceId, e.ExternalPaymentId })
                .IsUnique()
                .HasFilter("external_payment_id IS NOT NULL AND status <> 'Void'");
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

            entity.Property<uint>("xmin")
                .HasColumnType("xid")
                .IsRowVersion();

            entity.HasMany(e => e.LineItems)
                .WithOne(e => e.Receipt)
                .HasForeignKey(e => e.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.AuditEvents)
                .WithOne(e => e.Receipt)
                .HasForeignKey(e => e.ReceiptId)
                .OnDelete(DeleteBehavior.NoAction);
        });

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

            entity.Property<uint>("xmin")
                .HasColumnType("xid")
                .IsRowVersion();
        });

        modelBuilder.Entity<ReceiptAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ReceiptId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.RetainUntil);
            entity.HasIndex(e => e.EventType);

            entity.Property(e => e.EventType)
                .HasConversion<string>();

            entity.Property<uint>("xmin")
                .HasColumnType("xid")
                .IsRowVersion();
        });

        modelBuilder.Entity<InvoiceBalanceTracker>(entity =>
        {
            entity.HasKey(e => new { e.InvoiceId, e.SegmentId });

            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.RemainingBalance);

            entity.Property(e => e.TotalInvoiceAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalReceiptedAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.RemainingBalance)
                .HasColumnType("decimal(18,2)");

            entity.Property<uint>("xmin")
                .HasColumnType("xid")
                .IsRowVersion();
        });

        // PostgreSQL sequence for atomic receipt number generation
        modelBuilder.HasSequence<int>("receipt_number_seq")
            .StartsAt(1)
            .IncrementsBy(1);

        SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);
    }
}
