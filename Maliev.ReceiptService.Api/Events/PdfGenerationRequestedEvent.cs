namespace Maliev.ReceiptService.Api.Events;

/// <summary>
/// Event published to request PDF generation for a receipt
/// Per contracts/message-contracts.md
/// Routing Key: maliev.receipt.v1.pdf.requested
/// </summary>
public class PdfGenerationRequestedEvent
{
    public Guid ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
    public CustomerDetails CustomerDetails { get; set; } = new();
    public FinancialDetails FinancialDetails { get; set; } = new();
    public List<LineItemDto> LineItems { get; set; } = new();
    public TaxFields TaxFields { get; set; } = new();
    public string TemplateId { get; set; } = "receipt-v1";
}

public class CustomerDetails
{
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Address { get; set; }
}

public class FinancialDetails
{
    public DateTime IssueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal? WithholdingTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
}

public class LineItemDto
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
}

public class TaxFields
{
    public string TaxId { get; set; } = string.Empty;
    public decimal VatRate { get; set; }
    public string? WithholdingTaxType { get; set; }
}
