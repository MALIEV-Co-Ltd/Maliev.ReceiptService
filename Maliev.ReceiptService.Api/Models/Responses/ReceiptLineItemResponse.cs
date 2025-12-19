namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Receipt line item response DTO per contracts/receipts-api.yaml
/// </summary>
public class ReceiptLineItemResponse
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
}
