namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Response for customer payment behavior analytics
/// Per contracts/analytics-api.yaml - US5
/// </summary>
public class PaymentBehaviorResponse
{
    public Guid? CustomerId { get; set; }
    public required decimal AverageTimeToPayDays { get; set; }
    public required decimal PartialPaymentFrequency { get; set; }
    public required decimal VoidRate { get; set; }
    public required List<PaymentMethodStats> PaymentMethods { get; set; }
}

public class PaymentMethodStats
{
    public required string Method { get; set; }
    public required int Count { get; set; }
    public required decimal Percentage { get; set; }
}
