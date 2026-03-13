namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Response for payment completion rate analytics
/// Per contracts/analytics-api.yaml - US5
/// </summary>
public class PaymentCompletionResponse
{
    /// <summary>
    /// Gets or sets the period information.
    /// </summary>
    public required PeriodInfo Period { get; set; }

    /// <summary>
    /// Gets or sets the list of completion metrics.
    /// </summary>
    public required List<CompletionMetric> Metrics { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the metrics are estimated/placeholder values.
    /// </summary>
    public bool IsEstimated { get; set; }

    /// <summary>
    /// Gets or sets the optional date and time when the data was cached.
    /// </summary>
    public DateTime? CachedAt { get; set; }
}

/// <summary>
/// Represents period information.
/// </summary>
public class PeriodInfo
{
    /// <summary>
    /// Gets or sets the start date of the period.
    /// </summary>
    public required DateOnly Start { get; set; }

    /// <summary>
    /// Gets or sets the end date of the period.
    /// </summary>
    public required DateOnly End { get; set; }
}

/// <summary>
/// Represents completion metrics for a specific date.
/// </summary>
public class CompletionMetric
{
    /// <summary>
    /// Gets or sets the date.
    /// </summary>
    public required DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the total number of invoices.
    /// </summary>
    public required int TotalInvoices { get; set; }

    /// <summary>
    /// Gets or sets the number of receipted invoices.
    /// </summary>
    public required int ReceiptedInvoices { get; set; }

    /// <summary>
    /// Gets or sets the completion rate.
    /// </summary>
    public required decimal CompletionRate { get; set; }

    /// <summary>
    /// Gets or sets the average processing time.
    /// </summary>
    public required decimal AverageProcessingTime { get; set; }
}
