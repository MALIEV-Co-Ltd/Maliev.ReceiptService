namespace Maliev.ReceiptService.Application.DTOs.Responses;

public class PaymentCompletionResponse
{
    public PeriodInfo Period { get; set; } = new();
    public List<CompletionMetric> Metrics { get; set; } = new();
    public DateTime? CachedAt { get; set; }
}

public class PeriodInfo
{
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
}

public class CompletionMetric
{
    public DateOnly Date { get; set; }
    public int TotalInvoices { get; set; }
    public int ReceiptedInvoices { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageProcessingTime { get; set; }
}
