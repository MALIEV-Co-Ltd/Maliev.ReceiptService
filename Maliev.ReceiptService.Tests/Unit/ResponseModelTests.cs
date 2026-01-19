using Maliev.ReceiptService.Api.Models.Responses;
using Xunit;

namespace Maliev.ReceiptService.Tests.Unit;

public class ResponseModelTests
{
    [Fact]
    public void ResponseModels_Properties_AreAccessible()
    {
        var ageBreakdown = new AgeBreakdown { AgeRange = "0-30", Amount = 100, Count = 1 };
        Assert.Equal("0-30", ageBreakdown.AgeRange);
        Assert.Equal(100, ageBreakdown.Amount);
        Assert.Equal(1, ageBreakdown.Count);

        var completionMetric = new CompletionMetric { Date = DateOnly.FromDateTime(DateTime.UtcNow), TotalInvoices = 10, ReceiptedInvoices = 5, CompletionRate = 50, AverageProcessingTime = 1.5m };
        Assert.Equal(10, completionMetric.TotalInvoices);

        var metrics = new ProcessingMetricsResponse
        {
            Period = new TimePeriod { Start = DateTime.UtcNow, End = DateTime.UtcNow },
            ReceiptCreation = new ReceiptCreationMetrics
            {
                AverageDuration = 1.1m,
                P50Duration = 1.0m,
                P95Duration = 2.0m,
                P99Duration = 3.0m,
                TotalCreated = 100
            },
            InvoiceServiceCalls = new InvoiceServiceMetrics { AverageDuration = 0.5m, TimeoutCount = 1, RetryCount = 2 },
            PdfEventPublishing = new PdfEventMetrics { AverageDuration = 0.1m, FailureCount = 0 }
        };
        Assert.Equal(1.1m, metrics.ReceiptCreation.AverageDuration);
        Assert.Equal(0.5m, metrics.InvoiceServiceCalls.AverageDuration);
        Assert.Equal(0.1m, metrics.PdfEventPublishing.AverageDuration);
    }
}
