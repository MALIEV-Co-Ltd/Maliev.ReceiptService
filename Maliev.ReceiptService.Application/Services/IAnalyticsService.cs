using Maliev.ReceiptService.Application.Models.Responses;

namespace Maliev.ReceiptService.Application.Services;

/// <summary>
/// Analytics service interface for business metrics and payment analytics
/// Task: T095 [P] [US5] Create IAnalyticsService interface
/// Per contracts/analytics-api.yaml
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Gets payment completion rate metrics for a specified time period
    /// Calculation: (Total receipted invoices / Total invoices) × 100
    /// Caching: 5-minute Redis cache for performance
    /// </summary>
    Task<PaymentCompletionResponse> GetPaymentCompletionRateAsync(
        DateOnly startDate,
        DateOnly endDate,
        string groupBy = "month");

    /// <summary>
    /// Gets outstanding receivables (invoiced amount - receipted amount)
    /// Includes breakdown by age and top customers
    /// Caching: 5-minute Redis cache
    /// </summary>
    Task<OutstandingReceivablesResponse> GetOutstandingReceivablesAsync(
        DateOnly? asOfDate = null);

    /// <summary>
    /// Analyzes payment patterns for a specific customer or all customers
    /// Metrics: Average time to pay, partial payment frequency, void rate, payment methods
    /// Caching: 5-minute Redis cache
    /// </summary>
    Task<PaymentBehaviorResponse> GetCustomerPaymentBehaviorAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? customerId = null);

    /// <summary>
    /// Gets receipt creation performance metrics
    /// Metrics: Average creation time, P50/P95/P99 latencies, service call latencies
    /// Validates SC-001 (5s creation), SC-004 (1s PDF publish)
    /// </summary>
    Task<ProcessingMetricsResponse> GetProcessingMetricsAsync(
        DateOnly startDate,
        DateOnly endDate);
}
