using Maliev.ReceiptService.Application.DTOs.Responses;

namespace Maliev.ReceiptService.Application.Interfaces;

public interface IAnalyticsService
{
    Task<PaymentCompletionResponse> GetPaymentCompletionRateAsync(DateOnly startDate, DateOnly endDate, string groupBy = "month");
    Task<OutstandingReceivablesResponse> GetOutstandingReceivablesAsync(DateOnly? asOfDate = null);
    Task<PaymentBehaviorResponse> GetCustomerPaymentBehaviorAsync(DateOnly startDate, DateOnly endDate, Guid? customerId = null);
    Task<ProcessingMetricsResponse> GetProcessingMetricsAsync(DateTime fromDate, DateTime toDate);
}
