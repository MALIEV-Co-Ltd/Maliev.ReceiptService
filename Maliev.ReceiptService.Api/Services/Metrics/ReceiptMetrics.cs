using System.Diagnostics.Metrics;

namespace Maliev.ReceiptService.Api.Services.Metrics;

/// <summary>
/// Receipt service metrics for OpenTelemetry monitoring.
/// Records receipt creation counts and durations.
/// </summary>
public class ReceiptMetrics
{
    /// <summary>
    /// Gets the counter for total receipts created.
    /// </summary>
    public Counter<long> ReceiptsCreated { get; }

    /// <summary>
    /// Gets the histogram for receipt creation duration.
    /// </summary>
    public Histogram<double> CreationDuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory for creating metrics.</param>
    public ReceiptMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("receipts-meter");

        ReceiptsCreated = meter.CreateCounter<long>(
            "receipts.created.total",
            unit: "receipts",
            description: "Total number of receipts created");

        CreationDuration = meter.CreateHistogram<double>(
            "receipts.creation.duration",
            unit: "milliseconds",
            description: "Receipt creation duration");
    }
}
