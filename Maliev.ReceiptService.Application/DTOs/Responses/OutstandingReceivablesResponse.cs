namespace Maliev.ReceiptService.Application.DTOs.Responses;

public class OutstandingReceivablesResponse
{
    public DateOnly AsOfDate { get; set; }
    public decimal TotalOutstanding { get; set; }
    public string Currency { get; set; } = string.Empty;
    public List<AgeBreakdown> AgeBreakdown { get; set; } = new();
    public List<TopCustomer> TopCustomers { get; set; } = new();
}

public class AgeBreakdown
{
    public string AgeRange { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class TopCustomer
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal OutstandingAmount { get; set; }
    public int InvoiceCount { get; set; }
}
