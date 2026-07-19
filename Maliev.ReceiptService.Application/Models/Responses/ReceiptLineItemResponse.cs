namespace Maliev.ReceiptService.Application.Models.Responses;

/// <summary>
/// Receipt line item response DTO per contracts/receipts-api.yaml
/// </summary>
public class ReceiptLineItemResponse
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
    /// Gets or sets the total amount for this line.
    /// </summary>
    public decimal LineTotal { get; set; }
}
