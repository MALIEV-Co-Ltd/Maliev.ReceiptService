namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Response for payment completion rate analytics
/// Per contracts/analytics-api.yaml - US5
/// </summary>
public class PaymentCompletionResponse
{
    public required PeriodInfo Period { get; set; }
    public required List<CompletionMetric> Metrics { get; set; }
    public DateTime? CachedAt { get; set; }
}

public class PeriodInfo
{
    public required DateOnly Start { get; set; }
    public required DateOnly End { get; set; }
}

public class CompletionMetric
{
    public required DateOnly Date { get; set; }
    public required int TotalInvoices { get; set; }
    public required int ReceiptedInvoices { get; set; }
    public required decimal CompletionRate { get; set; }
    public required decimal AverageProcessingTime { get; set; }
}
