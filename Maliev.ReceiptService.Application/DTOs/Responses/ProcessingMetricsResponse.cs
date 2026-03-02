namespace Maliev.ReceiptService.Application.DTOs.Responses;

public class ProcessingMetricsResponse
{
    public TimePeriod Period { get; set; } = new();
    public ReceiptCreationMetrics ReceiptCreation { get; set; } = new();
    public InvoiceServiceMetrics InvoiceServiceCalls { get; set; } = new();
    public PdfEventMetrics PdfEventPublishing { get; set; } = new();
}

public class TimePeriod
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class ReceiptCreationMetrics
{
    public decimal AverageDuration { get; set; }
    public decimal P50Duration { get; set; }
    public decimal P95Duration { get; set; }
    public decimal P99Duration { get; set; }
    public int TotalCreated { get; set; }
}

public class InvoiceServiceMetrics
{
    public decimal AverageDuration { get; set; }
    public int TimeoutCount { get; set; }
    public int RetryCount { get; set; }
}

public class PdfEventMetrics
{
    public decimal AverageDuration { get; set; }
    public int FailureCount { get; set; }
}
