namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Response for processing time metrics
/// Per contracts/analytics-api.yaml - US5
/// </summary>
public class ProcessingMetricsResponse
{
    /// <summary>
    /// Gets or sets the time period for the metrics.
    /// </summary>
    public required TimePeriod Period { get; set; }

    /// <summary>
    /// Gets or sets the receipt creation metrics.
    /// </summary>
    public required ReceiptCreationMetrics ReceiptCreation { get; set; }

    /// <summary>
    /// Gets or sets the invoice service call metrics.
    /// </summary>
    public required InvoiceServiceMetrics InvoiceServiceCalls { get; set; }

    /// <summary>
    /// Gets or sets the PDF event publishing metrics.
    /// </summary>
    public required PdfEventMetrics PdfEventPublishing { get; set; }
}

/// <summary>
/// Represents a time period.
/// </summary>
public class TimePeriod
{
    /// <summary>
    /// Gets or sets the start date and time.
    /// </summary>
    public required DateTime Start { get; set; }

    /// <summary>
    /// Gets or sets the end date and time.
    /// </summary>
    public required DateTime End { get; set; }
}

/// <summary>
/// Represents receipt creation metrics.
/// </summary>
public class ReceiptCreationMetrics
{
    /// <summary>
    /// Gets or sets the average duration in milliseconds.
    /// </summary>
    public required decimal AverageDuration { get; set; }

    /// <summary>
    /// Gets or sets the 50th percentile duration.
    /// </summary>
    public required decimal P50Duration { get; set; }

    /// <summary>
    /// Gets or sets the 95th percentile duration.
    /// </summary>
    public required decimal P95Duration { get; set; }

    /// <summary>
    /// Gets or sets the 99th percentile duration.
    /// </summary>
    public required decimal P99Duration { get; set; }

    /// <summary>
    /// Gets or sets the total number of receipts created.
    /// </summary>
    public required int TotalCreated { get; set; }
}

/// <summary>
/// Represents invoice service metrics.
/// </summary>
public class InvoiceServiceMetrics
{
    /// <summary>
    /// Gets or sets the average duration in milliseconds.
    /// </summary>
    public required decimal AverageDuration { get; set; }

    /// <summary>
    /// Gets or sets the number of timeouts.
    /// </summary>
    public required int TimeoutCount { get; set; }

    /// <summary>
    /// Gets or sets the number of retries.
    /// </summary>
    public required int RetryCount { get; set; }
}

/// <summary>
/// Represents PDF event metrics.
/// </summary>
public class PdfEventMetrics
{
    /// <summary>
    /// Gets or sets the average duration in milliseconds.
    /// </summary>
    public required decimal AverageDuration { get; set; }

    /// <summary>
    /// Gets or sets the number of failures.
    /// </summary>
    public required int FailureCount { get; set; }
}
