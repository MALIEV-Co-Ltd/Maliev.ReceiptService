using System.ComponentModel.DataAnnotations;

namespace Maliev.ReceiptService.Application.DTOs.Requests;

public class CreateReceiptRequest
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    public Guid? InvoiceSegmentId { get; set; }
}
