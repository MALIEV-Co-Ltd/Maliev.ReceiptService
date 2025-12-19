namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Response for outstanding receivables analytics
/// Per contracts/analytics-api.yaml - US5
/// </summary>
public class OutstandingReceivablesResponse
{
    public required DateOnly AsOfDate { get; set; }
    public required decimal TotalOutstanding { get; set; }
    public required string Currency { get; set; }
    public required List<AgeBreakdown> AgeBreakdown { get; set; }
    public required List<TopCustomer> TopCustomers { get; set; }
}

public class AgeBreakdown
{
    public required string AgeRange { get; set; }
    public required decimal Amount { get; set; }
    public required int Count { get; set; }
}

public class TopCustomer
{
    public required string CustomerName { get; set; }
    public required decimal OutstandingAmount { get; set; }
    public required int InvoiceCount { get; set; }
}
