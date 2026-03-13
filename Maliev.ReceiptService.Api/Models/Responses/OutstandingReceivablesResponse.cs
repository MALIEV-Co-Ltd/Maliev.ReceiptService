namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Response for outstanding receivables analytics
/// Per contracts/analytics-api.yaml - US5
/// </summary>
public class OutstandingReceivablesResponse
{
    /// <summary>
    /// Gets or sets the as-of date for the report.
    /// </summary>
    public required DateOnly AsOfDate { get; set; }

    /// <summary>
    /// Gets or sets the total outstanding amount.
    /// </summary>
    public required decimal TotalOutstanding { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public required string Currency { get; set; }

    /// <summary>
    /// Gets or sets the aging breakdown.
    /// </summary>
    public required List<AgeBreakdown> AgeBreakdown { get; set; }

    /// <summary>
    /// Gets or sets the top customers by outstanding amount.
    /// </summary>
    public required List<TopCustomer> TopCustomers { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the metrics are estimated/placeholder values.
    /// </summary>
    public bool IsEstimated { get; set; }
}

/// <summary>
/// Represents aging breakdown details.
/// </summary>
public class AgeBreakdown
{
    /// <summary>
    /// Gets or sets the age range (e.g., 0-30 days).
    /// </summary>
    public required string AgeRange { get; set; }

    /// <summary>
    /// Gets or sets the outstanding amount for this range.
    /// </summary>
    public required decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the number of invoices in this range.
    /// </summary>
    public required int Count { get; set; }
}

/// <summary>
/// Represents top customer details.
/// </summary>
public class TopCustomer
{
    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public required string CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the total outstanding amount for the customer.
    /// </summary>
    public required decimal OutstandingAmount { get; set; }

    /// <summary>
    /// Gets or sets the number of outstanding invoices for the customer.
    /// </summary>
    public required int InvoiceCount { get; set; }
}
