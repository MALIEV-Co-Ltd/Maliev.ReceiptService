namespace Maliev.ReceiptService.Api.Models.Dtos;

public class InvoiceDto
{
    // Invoice Identity
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;

    // Customer Details
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerTaxId { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;

    // Financial Amounts
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal? WithholdingTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "THB";

    // Tax Compliance Fields
    public string SellerTaxId { get; set; } = string.Empty;
    public decimal VatRate { get; set; }
    public decimal? WithholdingTaxRate { get; set; }
    public string WithholdingTaxType { get; set; } = "None";

    // Line Items
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();

    // Split Invoice Support
    public List<InvoiceSegmentDto> Segments { get; set; } = new();

    // Payment Tracking
    public decimal TotalPaidAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;

    // Audit Fields
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class InvoiceLineItemDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
}

public class InvoiceSegmentDto
{
    public Guid SegmentId { get; set; }
    public string SegmentName { get; set; } = string.Empty;
    public decimal SegmentAmount { get; set; }
    public decimal SegmentTaxRate { get; set; }
    public decimal SegmentTotal { get; set; }
    public List<Guid> LineItemIds { get; set; } = new();
}
