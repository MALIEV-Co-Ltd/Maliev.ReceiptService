namespace Maliev.ReceiptService.Application.DTOs.Responses;

public class PaymentBehaviorResponse
{
    public Guid? CustomerId { get; set; }
    public decimal AverageTimeToPayDays { get; set; }
    public decimal PartialPaymentFrequency { get; set; }
    public decimal VoidRate { get; set; }
    public List<PaymentMethodStats> PaymentMethods { get; set; } = new();
}

public class PaymentMethodStats
{
    public string Method { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
