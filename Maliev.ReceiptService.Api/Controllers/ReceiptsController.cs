using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.ReceiptService.Api.Authorization;
using Maliev.ReceiptService.Application.Exceptions;
using Maliev.ReceiptService.Application.Models.Requests;
using Maliev.ReceiptService.Application.Authorization;
using Maliev.ReceiptService.Application.Services;
using Maliev.ReceiptService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.ReceiptService.Api.Controllers;

/// <summary>
/// Receipts API controller per contracts/receipts-api.yaml
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("receipt/v{version:apiVersion}/receipts")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ReceiptsController : ControllerBase
{
    private readonly IReceiptService _receiptService;
    private readonly ReceiptAccessGuard _accessGuard;
    private readonly ILogger<ReceiptsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptsController"/> class.
    /// </summary>
    /// <param name="receiptService">The receipt service.</param>
    /// <param name="accessGuard">Receipt object-scope access guard.</param>
    /// <param name="logger">The logger.</param>
    public ReceiptsController(
        IReceiptService receiptService,
        ReceiptAccessGuard accessGuard,
        ILogger<ReceiptsController> logger)
    {
        _receiptService = receiptService;
        _accessGuard = accessGuard;
        _logger = logger;
    }

    private string CurrentPrincipalId => _accessGuard.GetPrincipalId(User) ?? User.Identity?.Name ?? "system";

    private async Task<ReceiptAccessDecision> CheckReceiptAccessAsync(Guid id)
    {
        return await _accessGuard.CheckReceiptAsync(id, User, HttpContext.RequestAborted);
    }

    /// <summary>
    /// Create a new receipt
    /// POST /receipt/v1/receipts
    /// </summary>
    [HttpPost]
    [RequirePermission(ReceiptPermissions.Receipts.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateReceiptRequest request)
    {
        // Get correlation ID from middleware
        var correlationId = Guid.Parse(HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString());

        var accessScope = _accessGuard.GetScope(User);
        var staffId = CurrentPrincipalId;

        _logger.LogInformation("Creating receipt for invoice {InvoiceId}, correlation {CorrelationId}",
            request.InvoiceId, correlationId);

        try
        {
            var receipt = await _receiptService.CreateReceiptAsync(request, staffId, correlationId, accessScope);
            return CreatedAtAction(
                nameof(GetReceiptById),
                new { id = receipt.Id, version = "1.0" },
                receipt);
        }
        catch (InvoiceNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invoice not found: {InvoiceId}", request.InvoiceId);
            return NotFound(new
            {
                errorCode = "INVOICE_NOT_FOUND",
                message = ex.Message
            });
        }
        catch (TaxValidationException ex)
        {
            _logger.LogWarning(ex, "Tax validation failed for invoice {InvoiceId}", request.InvoiceId);
            return BadRequest(new
            {
                errorCode = "TAX_VALIDATION_FAILED",
                message = ex.Message
            });
        }
        catch (OverReceiptingException ex)
        {
            _logger.LogWarning(ex, "Over-receipting attempted for invoice {InvoiceId}", request.InvoiceId);
            return Conflict(new
            {
                errorCode = "INSUFFICIENT_BALANCE",
                message = ex.Message
            });
        }
        catch (DuplicateReceiptException ex)
        {
            _logger.LogWarning(ex, "Duplicate receipt or concurrency conflict for invoice {InvoiceId}", request.InvoiceId);
            return Conflict(new
            {
                errorCode = "CONCURRENCY_CONFLICT",
                message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Receipt creation denied for invoice {InvoiceId}", request.InvoiceId);
            return Forbid();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Invoice Service unavailable for invoice {InvoiceId}", request.InvoiceId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                errorCode = "INVOICE_SERVICE_UNAVAILABLE",
                message = "Unable to retrieve invoice details after 2 retries"
            });
        }
    }

    /// <summary>
    /// Get receipt by ID
    /// GET /v1/receipts/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ReceiptPermissions.Receipts.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceiptById(Guid id)
    {
        var accessDecision = await CheckReceiptAccessAsync(id);
        if (accessDecision == ReceiptAccessDecision.NotFound)
        {
            return NotFound(new
            {
                errorCode = "RECEIPT_NOT_FOUND",
                message = $"Receipt {id} not found"
            });
        }

        if (accessDecision == ReceiptAccessDecision.Forbidden) return Forbid();

        var receipt = await _receiptService.GetReceiptByIdAsync(id);

        if (receipt == null)
        {
            return NotFound(new
            {
                errorCode = "RECEIPT_NOT_FOUND",
                message = $"Receipt {id} not found"
            });
        }

        return Ok(receipt);
    }

    /// <summary>
    /// Query receipts with filtering, sorting, and pagination
    /// GET /v1/receipts
    /// Extended for US4 to support segment filtering (T082)
    /// </summary>
    [HttpGet]
    [RequirePermission(ReceiptPermissions.Receipts.Query)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QueryReceipts(
        [FromQuery] Guid? invoiceId = null,
        [FromQuery] ReceiptStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? segmentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "issueDate",
        [FromQuery] string sortOrder = "desc")
    {
        // Validate parameters
        if (page < 1)
        {
            return BadRequest(new
            {
                errorCode = "INVALID_PARAMETER",
                message = "Page must be greater than 0"
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                errorCode = "INVALID_PARAMETER",
                message = "Page size must be between 1 and 100"
            });
        }

        var validSortFields = new[] { "issuedate", "receiptnumber", "totalamount" };
        if (!validSortFields.Contains(sortBy.ToLower()))
        {
            return BadRequest(new
            {
                errorCode = "INVALID_PARAMETER",
                message = "Sort by must be one of: issueDate, receiptNumber, totalAmount"
            });
        }

        var validSortOrders = new[] { "asc", "desc" };
        if (!validSortOrders.Contains(sortOrder.ToLower()))
        {
            return BadRequest(new
            {
                errorCode = "INVALID_PARAMETER",
                message = "Sort order must be 'asc' or 'desc'"
            });
        }

        // Convert DateTime parameters to UTC if they have Unspecified kind (from query string parsing)
        var fromDateUtc = fromDate.HasValue && fromDate.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc)
            : fromDate;

        var toDateUtc = toDate.HasValue && toDate.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc)
            : toDate;

        var result = await _receiptService.QueryReceiptsAsync(
            invoiceId, status, fromDateUtc, toDateUtc, segmentId, page, pageSize, sortBy, sortOrder, _accessGuard.GetScope(User));

        return Ok(result);
    }

    /// <summary>
    /// Void a receipt
    /// POST /v1/receipts/{id}/void
    /// Task: T071 [P] [US3] Implement POST /v1/receipts/{id}/void
    /// </summary>
    [HttpPost("{id:guid}/void")]
    [RequirePermission(ReceiptPermissions.Receipts.Void, IsCritical = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> VoidReceipt(Guid id, [FromBody] VoidReceiptRequest request)
    {
        // Get correlation ID from middleware
        var correlationId = Guid.Parse(HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString());

        var staffId = CurrentPrincipalId;

        _logger.LogInformation(
            "Voiding receipt {ReceiptId}, reason: {Reason}, correlation {CorrelationId}",
            id, request.Reason, correlationId);

        try
        {
            var accessDecision = await CheckReceiptAccessAsync(id);
            if (accessDecision == ReceiptAccessDecision.NotFound)
            {
                return NotFound(new
                {
                    errorCode = "RECEIPT_NOT_FOUND",
                    message = $"Receipt {id} not found"
                });
            }

            if (accessDecision == ReceiptAccessDecision.Forbidden) return Forbid();

            var receipt = await _receiptService.VoidReceiptAsync(id, request.Reason, staffId, correlationId);
            return Ok(receipt);
        }
        catch (ReceiptNotFoundException ex)
        {
            _logger.LogWarning(ex, "Receipt not found: {ReceiptId}", id);
            return NotFound(new
            {
                errorCode = "RECEIPT_NOT_FOUND",
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already voided"))
        {
            _logger.LogWarning(ex, "Receipt {ReceiptId} already voided", id);
            return Conflict(new
            {
                errorCode = "RECEIPT_ALREADY_VOIDED",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Send a receipt to customer via specified channel
    /// POST /v1/receipts/{id}/send
    /// </summary>
    [HttpPost("{id:guid}/send")]
    [RequirePermission(ReceiptPermissions.Receipts.Send)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendReceipt(Guid id, [FromBody] SendReceiptRequest request)
    {
        // Get correlation ID from middleware
        var correlationId = Guid.Parse(HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString());

        var staffId = CurrentPrincipalId;

        _logger.LogInformation(
            "Sending receipt {ReceiptId} to {Destination} via {Channel}, correlation {CorrelationId}",
            id, request.Destination, request.Channel, correlationId);

        try
        {
            var accessDecision = await CheckReceiptAccessAsync(id);
            if (accessDecision == ReceiptAccessDecision.NotFound)
            {
                return NotFound(new
                {
                    errorCode = "RECEIPT_NOT_FOUND",
                    message = $"Receipt {id} not found"
                });
            }

            if (accessDecision == ReceiptAccessDecision.Forbidden) return Forbid();

            var receipt = await _receiptService.SendReceiptAsync(
                id, request.Destination, request.Channel, staffId, correlationId);
            return Ok(receipt);
        }
        catch (ReceiptNotFoundException ex)
        {
            _logger.LogWarning(ex, "Receipt not found: {ReceiptId}", id);
            return NotFound(new
            {
                errorCode = "RECEIPT_NOT_FOUND",
                message = ex.Message
            });
        }
        catch (ReceiptBusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Cannot send receipt {ReceiptId}: {Message}", id, ex.Message);
            return BadRequest(new
            {
                errorCode = "INVALID_OPERATION",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get audit history for a receipt
    /// GET /v1/receipts/{id}/audit-history
    /// Task: T072 [P] [US3] Implement GET /v1/receipts/{id}/audit-history
    /// </summary>
    [HttpGet("{id:guid}/audit-history")]
    [RequirePermission(ReceiptPermissions.Audit.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditHistory(Guid id)
    {
        _logger.LogInformation("Retrieving audit history for receipt {ReceiptId}", id);

        try
        {
            var accessDecision = await CheckReceiptAccessAsync(id);
            if (accessDecision == ReceiptAccessDecision.NotFound)
            {
                return NotFound(new
                {
                    errorCode = "RECEIPT_NOT_FOUND",
                    message = $"Receipt {id} not found"
                });
            }

            if (accessDecision == ReceiptAccessDecision.Forbidden) return Forbid();

            var auditEvents = await _receiptService.GetAuditHistoryAsync(id);
            return Ok(auditEvents);
        }
        catch (ReceiptNotFoundException ex)
        {
            _logger.LogWarning(ex, "Receipt not found: {ReceiptId}", id);
            return NotFound(new
            {
                errorCode = "RECEIPT_NOT_FOUND",
                message = ex.Message
            });
        }
    }
}
