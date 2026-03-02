namespace Maliev.ReceiptService.Application.DTOs.Responses;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerTaxId { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal? WithholdingTaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "THB";
    public string SellerTaxId { get; set; } = string.Empty;
    public decimal VatRate { get; set; }
    public decimal? WithholdingTaxRate { get; set; }
    public string WithholdingTaxType { get; set; } = "None";
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();
    public List<InvoiceSegmentDto> Segments { get; set; } = new();
    public decimal TotalPaidAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
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
