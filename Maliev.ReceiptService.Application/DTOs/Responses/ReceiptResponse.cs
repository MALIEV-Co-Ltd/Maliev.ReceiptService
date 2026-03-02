namespace Maliev.ReceiptService.Application.DTOs.Responses;

public class ReceiptResponse
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public Guid? InvoiceSegmentId { get; set; }
    public DateTime IssueDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxId { get; set; }
    public string? CustomerAddress { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal? WithholdingTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? PdfReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? VoidedAt { get; set; }
    public string? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public List<ReceiptLineItemResponse> LineItems { get; set; } = new();
}
