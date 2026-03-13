using System.ComponentModel.DataAnnotations;

namespace Maliev.ReceiptService.Application.Models.Requests;

/// <summary>
/// Request to create a new receipt for a paid or partially paid invoice
/// Per contracts/receipts-api.yaml
/// </summary>
public class CreateReceiptRequest
{
    /// <summary>
    /// Invoice ID from Invoice Service
    /// </summary>
    [Required]
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Receipt amount (can be partial or full invoice amount)
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method (e.g., Cash, Bank Transfer, Credit Card)
    /// </summary>
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Optional invoice segment ID for split invoice receipts (US4)
    /// If provided, the receipt is for a specific segment of a split invoice
    /// </summary>
    public Guid? InvoiceSegmentId { get; set; }
}
