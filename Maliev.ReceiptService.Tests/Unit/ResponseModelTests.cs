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

    [Fact]
    public void TopCustomer_Model_HasRequiredProperties()
    {
        var topCustomer = new TopCustomer
        {
            CustomerName = "Test Customer",
            OutstandingAmount = 50000m,
            InvoiceCount = 10
        };

        Assert.Equal("Test Customer", topCustomer.CustomerName);
        Assert.Equal(50000m, topCustomer.OutstandingAmount);
        Assert.Equal(10, topCustomer.InvoiceCount);
    }

    [Fact]
    public void PaymentMethodStats_Model_HasRequiredProperties()
    {
        var stats = new PaymentMethodStats
        {
            Method = "Cash",
            Count = 25,
            Percentage = 50.5m
        };

        Assert.Equal("Cash", stats.Method);
        Assert.Equal(25, stats.Count);
        Assert.Equal(50.5m, stats.Percentage);
    }

    [Fact]
    public void PaymentBehaviorResponse_Model_HasAllProperties()
    {
        var response = new PaymentBehaviorResponse
        {
            CustomerId = Guid.NewGuid(),
            AverageTimeToPayDays = 15.5m,
            PartialPaymentFrequency = 12.3m,
            VoidRate = 5.0m,
            PaymentMethods = new List<PaymentMethodStats>
            {
                new() { Method = "Cash", Count = 10, Percentage = 50m },
                new() { Method = "Credit Card", Count = 10, Percentage = 50m }
            }
        };

        Assert.NotEqual(Guid.Empty, response.CustomerId);
        Assert.Equal(15.5m, response.AverageTimeToPayDays);
        Assert.Equal(12.3m, response.PartialPaymentFrequency);
        Assert.Equal(5.0m, response.VoidRate);
        Assert.Equal(2, response.PaymentMethods.Count);
    }

    [Fact]
    public void OutstandingReceivablesResponse_Model_HasAllProperties()
    {
        var response = new OutstandingReceivablesResponse
        {
            AsOfDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TotalOutstanding = 100000m,
            Currency = "THB",
            AgeBreakdown = new List<AgeBreakdown>
            {
                new() { AgeRange = "0-30 days", Amount = 50000m, Count = 10 },
                new() { AgeRange = "31-60 days", Amount = 30000m, Count = 5 }
            },
            TopCustomers = new List<TopCustomer>
            {
                new() { CustomerName = "Customer A", OutstandingAmount = 20000m, InvoiceCount = 3 },
                new() { CustomerName = "Customer B", OutstandingAmount = 15000m, InvoiceCount = 2 }
            }
        };

        Assert.Equal(100000m, response.TotalOutstanding);
        Assert.Equal("THB", response.Currency);
        Assert.Equal(2, response.AgeBreakdown.Count);
        Assert.Equal(2, response.TopCustomers.Count);
    }

    [Fact]
    public void AuditEvent_Model_HasAllProperties()
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            ReceiptId = Guid.NewGuid(),
            EventType = "Created",
            Timestamp = DateTime.UtcNow,
            StaffMemberId = "staff-001",
            Reason = "Test reason",
            PreviousState = "{}",
            NewState = "{}",
            CorrelationId = Guid.NewGuid(),
            RetainUntil = DateTime.UtcNow.AddYears(7)
        };

        Assert.NotEqual(Guid.Empty, auditEvent.Id);
        Assert.Equal("Created", auditEvent.EventType);
        Assert.Equal("staff-001", auditEvent.StaffMemberId);
        Assert.NotEqual(DateTime.MinValue, auditEvent.RetainUntil);
    }
}
