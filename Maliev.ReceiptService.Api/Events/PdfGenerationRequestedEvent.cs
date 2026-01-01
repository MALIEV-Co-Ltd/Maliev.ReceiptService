namespace Maliev.ReceiptService.Api.Events;

/// <summary>
/// Event published to request PDF generation for a receipt
/// Per contracts/message-contracts.md
/// Routing Key: maliev.receipt.v1.pdf.requested
/// </summary>
public class PdfGenerationRequestedEvent
{
    /// <summary>
    /// Gets or sets the receipt ID.
    /// </summary>
    public Guid ReceiptId { get; set; }

    /// <summary>
    /// Gets or sets the receipt number.
    /// </summary>
    public string ReceiptNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the customer details.
    /// </summary>
    public CustomerDetails CustomerDetails { get; set; } = new();

    /// <summary>
    /// Gets or sets the financial details.
    /// </summary>
    public FinancialDetails FinancialDetails { get; set; } = new();

    /// <summary>
    /// Gets or sets the line items.
    /// </summary>
    public List<LineItemDto> LineItems { get; set; } = new();

    /// <summary>
    /// Gets or sets the tax fields.
    /// </summary>
    public TaxFields TaxFields { get; set; } = new();

    /// <summary>
    /// Gets or sets the template ID.
    /// </summary>
    public string TemplateId { get; set; } = "receipt-v1";
}

/// <summary>
/// Represents customer details for PDF generation.
/// </summary>
public class CustomerDetails
{
    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer tax ID.
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Gets or sets the customer address.
    /// </summary>
    public string? Address { get; set; }
}

/// <summary>
/// Represents financial details for PDF generation.
/// </summary>
public class FinancialDetails
{
    /// <summary>
    /// Gets or sets the issue date.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the subtotal.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Gets or sets the tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the withholding tax amount.
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
}

/// <summary>
/// Represents a line item DTO for PDF generation.
/// </summary>
public class LineItemDto
{
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
    /// Gets or sets the line total.
    /// </summary>
    public decimal LineTotal { get; set; }
}

/// <summary>
/// Represents tax-related fields for PDF generation.
/// </summary>
public class TaxFields
{
    /// <summary>
    /// Gets or sets the tax ID.
    /// </summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the VAT rate.
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// Gets or sets the withholding tax type.
    /// </summary>
    public string? WithholdingTaxType { get; set; }
}
