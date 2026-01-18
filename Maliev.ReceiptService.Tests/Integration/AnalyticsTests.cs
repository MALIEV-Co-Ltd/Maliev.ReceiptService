using System.Net;
using System.Net.Http.Json;
using Maliev.ReceiptService.Api.Models.Responses;
using Maliev.ReceiptService.Api.Services.IAM;
using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

[Collection("IntegrationTests")]
public class AnalyticsTests : BaseReceiptIntegrationTest
{
    public AnalyticsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetPaymentCompletionRate_ReturnsSuccess()
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await Client.GetAsync($"/receipt/v1/analytics/payment-completion?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaymentCompletionResponse>();
        Assert.NotNull(result);
        Assert.Equal(startDate, result.Period.Start);
        Assert.NotEmpty(result.Metrics);
    }

    [Fact]
    public async Task GetPaymentCompletionRate_InvalidDateRange_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/receipt/v1/analytics/payment-completion?startDate=2023-01-31&endDate=2023-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPaymentCompletionRate_InvalidGroupBy_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/receipt/v1/analytics/payment-completion?startDate=2023-01-01&endDate=2023-01-31&groupBy=invalid");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOutstandingReceivables_ReturnsSuccess()
    {
        var response = await Client.GetAsync("/receipt/v1/analytics/outstanding-receivables");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OutstandingReceivablesResponse>();
        Assert.NotNull(result);
        Assert.Equal("THB", result.Currency);
        Assert.NotEmpty(result.AgeBreakdown);
    }

    [Fact]
    public async Task GetCustomerPaymentBehavior_ReturnsSuccess()
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await Client.GetAsync($"/receipt/v1/analytics/customer-payment-behavior?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaymentBehaviorResponse>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetCustomerPaymentBehavior_InvalidDateRange_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/receipt/v1/analytics/customer-payment-behavior?startDate=2023-01-31&endDate=2023-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProcessingMetrics_ReturnsSuccess()
    {
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;
        startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        var response = await Client.GetAsync($"/receipt/v1/analytics/processing-metrics?startDate={startDate:O}&endDate={endDate:O}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProcessingMetricsResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.ReceiptCreation);
        Assert.NotNull(result.InvoiceServiceCalls);
        Assert.NotNull(result.PdfEventPublishing);
    }

    [Fact]
    public async Task GetProcessingMetrics_InvalidDateRange_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/receipt/v1/analytics/processing-metrics?startDate=2023-01-31&endDate=2023-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
