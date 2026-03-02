using Maliev.ReceiptService.Api.Models.Requests;
using Maliev.ReceiptService.Api.Models.Responses;
using Maliev.ReceiptService.Domain.Enums;

namespace Maliev.ReceiptService.Api.Services;

/// <summary>
/// Receipt service interface for managing receipt operations
/// </summary>
public interface IReceiptService
{
    /// <summary>
    /// Creates a new receipt for a paid or partially paid invoice
    /// </summary>
    /// <param name="request">Receipt creation request</param>
    /// <param name="staffId">Staff member creating the receipt</param>
    /// <param name="correlationId">Correlation ID for distributed tracing</param>
    /// <returns>Created receipt response</returns>
    Task<ReceiptResponse> CreateReceiptAsync(
        CreateReceiptRequest request,
        string staffId,
        Guid correlationId);

    /// <summary>
    /// Retrieves a receipt by its ID
    /// </summary>
    /// <param name="id">Receipt ID</param>
    /// <returns>Receipt response or null if not found</returns>
    Task<ReceiptResponse?> GetReceiptByIdAsync(Guid id);

    /// <summary>
    /// Queries receipts with filtering, sorting, and pagination
    /// Per contracts/receipts-api.yaml
    /// Extended for US4 to support segment filtering (T082)
    /// </summary>
    Task<PagedResponse<ReceiptResponse>> QueryReceiptsAsync(
        Guid? invoiceId = null,
        ReceiptStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? segmentId = null,
        int page = 1,
        int pageSize = 20,
        string sortBy = "issueDate",
        string sortOrder = "desc");

    /// <summary>
    /// Voids a receipt with balance restoration and audit trail
    /// Task: T046 [P] [US3] Create IReceiptService interface extension
    /// </summary>
    /// <param name="receiptId">Receipt ID to void</param>
    /// <param name="reason">Reason for voiding</param>
    /// <param name="staffId">Staff member voiding the receipt</param>
    /// <param name="correlationId">Correlation ID for distributed tracing</param>
    /// <returns>Voided receipt response</returns>
    Task<ReceiptResponse> VoidReceiptAsync(
        Guid receiptId,
        string reason,
        string staffId,
        Guid correlationId);

    /// <summary>
    /// Gets complete audit history for a receipt
    /// Task: T072 [P] [US3] Implement GET /v1/receipts/{id}/audit-history
    /// </summary>
    /// <param name="receiptId">Receipt ID</param>
    /// <returns>List of audit events in chronological order</returns>
    Task<List<AuditEvent>> GetAuditHistoryAsync(Guid receiptId);

    /// <summary>
    /// Sends a receipt to a customer via specified channel
    /// </summary>
    /// <param name="receiptId">Receipt ID to send</param>
    /// <param name="destination">Destination (email, phone number, etc.)</param>
    /// <param name="channel">Delivery channel (Email, SMS, LINE, WhatsApp)</param>
    /// <param name="staffId">Staff member initiating the send</param>
    /// <param name="correlationId">Correlation ID for distributed tracing</param>
    /// <returns>Receipt response</returns>
    Task<ReceiptResponse> SendReceiptAsync(
        Guid receiptId,
        string destination,
        string channel,
        string staffId,
        Guid correlationId);
}
