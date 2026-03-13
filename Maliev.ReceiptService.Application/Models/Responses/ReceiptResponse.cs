namespace Maliev.ReceiptService.Application.Models.Responses;

/// <summary>
/// Receipt response DTO per contracts/receipts-api.yaml
/// </summary>
public class ReceiptResponse
{
    /// <summary>
    /// Gets or sets the receipt ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the receipt number.
    /// </summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invoice ID.
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Gets or sets the optional invoice segment ID.
    /// </summary>
    public Guid? InvoiceSegmentId { get; set; }

    /// <summary>
    /// Gets or sets the issue date.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer tax ID.
    /// </summary>
    public string? CustomerTaxId { get; set; }

    /// <summary>
    /// Gets or sets the customer address.
    /// </summary>
    public string? CustomerAddress { get; set; }

    /// <summary>
    /// Gets or sets the subtotal.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Gets or sets the tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the optional withholding tax amount.
    /// </summary>
    public decimal? WithholdingTaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment method.
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Gets or sets the receipt status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional PDF reference ID.
    /// </summary>
    public Guid? PdfReferenceId { get; set; }

    /// <summary>
    /// Gets or sets the creation date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the receipt.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional date and time when voided.
    /// </summary>
    public DateTime? VoidedAt { get; set; }

    /// <summary>
    /// Gets or sets the optional user who voided the receipt.
    /// </summary>
    public string? VoidedBy { get; set; }

    /// <summary>
    /// Gets or sets the optional reason for voiding.
    /// </summary>
    public string? VoidReason { get; set; }

    /// <summary>
    /// Gets or sets the line items.
    /// </summary>
    public List<ReceiptLineItemResponse> LineItems { get; set; } = new();
}
