using Maliev.ReceiptService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Maliev.ReceiptService.Domain.Entities;

public class Receipt
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    [MaxLength(50)]
    public string ReceiptNumber { get; init; } = string.Empty;

    [Required]
    public Guid InvoiceId { get; init; }

    public Guid? ExternalPaymentId { get; init; }

    public Guid? InvoiceSegmentId { get; init; }

    [Required]
    public DateTime IssueDate { get; init; }

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; init; } = string.Empty;

    [MaxLength(50)]
    public string? CustomerTaxId { get; init; }

    [MaxLength(500)]
    public string? CustomerAddress { get; init; }

    [Required]
    public decimal Subtotal { get; init; }

    [Required]
    public decimal TaxAmount { get; init; }

    public decimal? WithholdingTaxAmount { get; init; }

    [Required]
    public decimal TotalAmount { get; init; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; init; } = "THB";

    [MaxLength(50)]
    public string? PaymentMethod { get; init; }

    [Required]
    public ReceiptStatus Status { get; set; }

    public Guid? PdfReferenceId { get; set; }

    [Required]
    public DateTime CreatedAt { get; init; }

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; init; } = string.Empty;

    public DateTime? VoidedAt { get; set; }

    [MaxLength(100)]
    public string? VoidedBy { get; set; }

    [MaxLength(500)]
    public string? VoidReason { get; set; }

    [Required]
    public Guid CorrelationId { get; init; }

    public ICollection<ReceiptLineItem> LineItems { get; init; } = new List<ReceiptLineItem>();
    public ICollection<ReceiptAuditEvent> AuditEvents { get; init; } = new List<ReceiptAuditEvent>();

    public void Void(string voidedBy, string reason)
    {
        if (Status == ReceiptStatus.Void)
        {
            throw new InvalidOperationException("Receipt is already voided");
        }

        Status = ReceiptStatus.Void;
        VoidedAt = DateTime.UtcNow;
        VoidedBy = voidedBy;
        VoidReason = reason;
    }
}
