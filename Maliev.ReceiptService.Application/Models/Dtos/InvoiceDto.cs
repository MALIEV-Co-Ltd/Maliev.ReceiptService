namespace Maliev.ReceiptService.Application.Models.Dtos;

/// <summary>
/// Data Transfer Object for an invoice.
/// </summary>
public class InvoiceDto
{
    /// <summary>
    /// Gets or sets the invoice ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the invoice number.
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issue date.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the optional due date.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Gets or sets the invoice status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer ID.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer tax ID.
    /// </summary>
    public string CustomerTaxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer address.
    /// </summary>
    public string CustomerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subtotal amount.
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
    /// Gets or sets the currency (default is THB).
    /// </summary>
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Gets or sets the seller's tax ID.
    /// </summary>
    public string SellerTaxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the VAT rate.
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// Gets or sets the optional withholding tax rate.
    /// </summary>
    public decimal? WithholdingTaxRate { get; set; }

    /// <summary>
    /// Gets or sets the withholding tax type (default is None).
    /// </summary>
    public string WithholdingTaxType { get; set; } = "None";

    /// <summary>
    /// Gets or sets the line items.
    /// </summary>
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();

    /// <summary>
    /// Gets or sets the invoice segments for split invoices.
    /// </summary>
    public List<InvoiceSegmentDto> Segments { get; set; } = new();

    /// <summary>
    /// Gets or sets the total paid amount.
    /// </summary>
    public decimal TotalPaidAmount { get; set; }

    /// <summary>
    /// Gets or sets the remaining balance.
    /// </summary>
    public decimal RemainingBalance { get; set; }

    /// <summary>
    /// Gets or sets the payment status.
    /// </summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the invoice was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the invoice.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for an invoice line item.
/// </summary>
public class InvoiceLineItemDto
{
    /// <summary>
    /// Gets or sets the line item ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the tax rate.
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Gets or sets the total amount for this line.
    /// </summary>
    public decimal LineTotal { get; set; }
}

/// <summary>
/// Data Transfer Object for an invoice segment.
/// </summary>
public class InvoiceSegmentDto
{
    /// <summary>
    /// Gets or sets the segment ID.
    /// </summary>
    public Guid SegmentId { get; set; }

    /// <summary>
    /// Gets or sets the segment name.
    /// </summary>
    public string SegmentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base amount for this segment.
    /// </summary>
    public decimal SegmentAmount { get; set; }

    /// <summary>
    /// Gets or sets the tax rate for this segment.
    /// </summary>
    public decimal SegmentTaxRate { get; set; }

    /// <summary>
    /// Gets or sets the total amount for this segment including tax.
    /// </summary>
    public decimal SegmentTotal { get; set; }

    /// <summary>
    /// Gets or sets the list of line item IDs included in this segment.
    /// </summary>
    public List<Guid> LineItemIds { get; set; } = new();
}
