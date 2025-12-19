using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.ReceiptService.Data.Models.Entities;

/// <summary>
/// Tracks receiptable balance for invoices and invoice segments (US4)
/// Uses composite key (InvoiceId + SegmentId) to support segment-level tracking
/// SegmentId = null for whole-invoice tracking
/// </summary>
public class InvoiceBalanceTracker
{
    [Key]
    [Column(Order = 0)]
    public Guid InvoiceId { get; init; }

    /// <summary>
    /// Segment ID for split invoice tracking (Guid.Empty for whole invoice)
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public Guid SegmentId { get; init; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalInvoiceAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalReceiptedAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal RemainingBalance { get; set; }

    [Required]
    public DateTime LastUpdatedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
