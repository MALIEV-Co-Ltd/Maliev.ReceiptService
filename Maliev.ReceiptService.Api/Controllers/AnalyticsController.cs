using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Maliev.ReceiptService.Api.Services;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.ReceiptService.Api.Services.IAM;

namespace Maliev.ReceiptService.Api.Controllers;

/// <summary>
/// Analytics and business metrics endpoints
/// Tasks: T097-T100 [P] [US5] Implement analytics endpoints
/// Per contracts/analytics-api.yaml
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("receipt/v{version:apiVersion}/analytics")]
[RequirePermission(ReceiptPermissions.Receipts.Query)] // Base permission for analytics? Or Audit?
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get payment completion rate
    /// GET /v1/analytics/payment-completion
    /// Task: T097 [P] [US5]
    /// </summary>
    [HttpGet("payment-completion")]
    [RequirePermission(ReceiptPermissions.Audit.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaymentCompletionRate(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] string groupBy = "month")
    {
        if (endDate < startDate)
        {
            return BadRequest(new
            {
                errorCode = "INVALID_DATE_RANGE",
                message = "End date must be greater than or equal to start date"
            });
        }

        var validGroupBy = new[] { "day", "week", "month" };
        if (!validGroupBy.Contains(groupBy.ToLower()))
        {
            return BadRequest(new
            {
                errorCode = "INVALID_PARAMETER",
                message = $"GroupBy must be one of: {string.Join(", ", validGroupBy)}"
            });
        }

        _logger.LogInformation(
            "Payment completion rate requested: {StartDate} to {EndDate}, groupBy: {GroupBy}",
            startDate, endDate, groupBy);

        var result = await _analyticsService.GetPaymentCompletionRateAsync(startDate, endDate, groupBy);
        return Ok(result);
    }

    /// <summary>
    /// Get outstanding receivables
    /// GET /v1/analytics/outstanding-receivables
    /// Task: T098 [P] [US5]
    /// </summary>
    [HttpGet("outstanding-receivables")]
    [RequirePermission(ReceiptPermissions.Audit.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutstandingReceivables(
        [FromQuery] DateOnly? asOf = null)
    {
        _logger.LogInformation("Outstanding receivables requested: asOf {AsOfDate}", asOf);

        var result = await _analyticsService.GetOutstandingReceivablesAsync(asOf);
        return Ok(result);
    }

    /// <summary>
    /// Get customer payment behavior
    /// GET /v1/analytics/customer-payment-behavior
    /// Task: T099 [P] [US5]
    /// </summary>
    [HttpGet("customer-payment-behavior")]
    [RequirePermission(ReceiptPermissions.Audit.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCustomerPaymentBehavior(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] Guid? customerId = null)
    {
        if (endDate < startDate)
        {
            return BadRequest(new
            {
                errorCode = "INVALID_DATE_RANGE",
                message = "End date must be greater than or equal to start date"
            });
        }

        _logger.LogInformation(
            "Customer payment behavior requested: {StartDate} to {EndDate}, customer: {CustomerId}",
            startDate, endDate, customerId);

        var result = await _analyticsService.GetCustomerPaymentBehaviorAsync(startDate, endDate, customerId);
        return Ok(result);
    }

    /// <summary>
    /// Get processing time metrics
    /// GET /v1/analytics/processing-metrics
    /// Task: T100 [P] [US5]
    /// </summary>
    [HttpGet("processing-metrics")]
    [RequirePermission(ReceiptPermissions.Audit.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProcessingMetrics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (endDate < startDate)
        {
            return BadRequest(new
            {
                errorCode = "INVALID_DATE_RANGE",
                message = "End date must be greater than or equal to start date"
            });
        }

        _logger.LogInformation(
            "Processing metrics requested: {StartDate} to {EndDate}",
            startDate, endDate);

        var result = await _analyticsService.GetProcessingMetricsAsync(startDate, endDate);
        return Ok(result);
    }
}
