using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.ReceiptService.Domain.Entities;

public class InvoiceBalanceTracker
{
    [Key]
    [Column(Order = 0)]
    public Guid InvoiceId { get; init; }

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
