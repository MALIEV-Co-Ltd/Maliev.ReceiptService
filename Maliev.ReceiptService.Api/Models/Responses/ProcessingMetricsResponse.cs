namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Response for processing time metrics
/// Per contracts/analytics-api.yaml - US5
/// </summary>
public class ProcessingMetricsResponse
{
    public required TimePeriod Period { get; set; }
    public required ReceiptCreationMetrics ReceiptCreation { get; set; }
    public required InvoiceServiceMetrics InvoiceServiceCalls { get; set; }
    public required PdfEventMetrics PdfEventPublishing { get; set; }
}

public class TimePeriod
{
    public required DateTime Start { get; set; }
    public required DateTime End { get; set; }
}

public class ReceiptCreationMetrics
{
    public required decimal AverageDuration { get; set; }
    public required decimal P50Duration { get; set; }
    public required decimal P95Duration { get; set; }
    public required decimal P99Duration { get; set; }
    public required int TotalCreated { get; set; }
}

public class InvoiceServiceMetrics
{
    public required decimal AverageDuration { get; set; }
    public required int TimeoutCount { get; set; }
    public required int RetryCount { get; set; }
}

public class PdfEventMetrics
{
    public required decimal AverageDuration { get; set; }
    public required int FailureCount { get; set; }
}
