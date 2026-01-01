namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Response for customer payment behavior analytics
/// Per contracts/analytics-api.yaml - US5
/// </summary>
public class PaymentBehaviorResponse
{
    /// <summary>
    /// Gets or sets the customer ID (null if for all customers).
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the average time to pay in days.
    /// </summary>
    public required decimal AverageTimeToPayDays { get; set; }

    /// <summary>
    /// Gets or sets the frequency of partial payments.
    /// </summary>
    public required decimal PartialPaymentFrequency { get; set; }

    /// <summary>
    /// Gets or sets the void rate.
    /// </summary>
    public required decimal VoidRate { get; set; }

    /// <summary>
    /// Gets or sets the payment method statistics.
    /// </summary>
    public required List<PaymentMethodStats> PaymentMethods { get; set; }
}

/// <summary>
/// Represents payment method statistics.
/// </summary>
public class PaymentMethodStats
{
    /// <summary>
    /// Gets or sets the payment method name.
    /// </summary>
    public required string Method { get; set; }

    /// <summary>
    /// Gets or sets the count of payments.
    /// </summary>
    public required int Count { get; set; }

    /// <summary>
    /// Gets or sets the percentage of total payments.
    /// </summary>
    public required decimal Percentage { get; set; }
}
