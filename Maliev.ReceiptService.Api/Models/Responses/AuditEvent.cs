namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Response model for receipt audit event
/// Task: T068 [P] [US3] Create AuditEvent DTO for audit history responses
/// </summary>
public class AuditEvent
{
    /// <summary>
    /// Unique identifier for the audit event
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Receipt ID this event belongs to
    /// </summary>
    public Guid ReceiptId { get; set; }

    /// <summary>
    /// Type of audit event (Created, Voided, PdfGenerated, Corrected)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// When the event occurred (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Staff member who performed the action
    /// </summary>
    public string StaffMemberId { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the action (e.g., void reason)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// JSON snapshot of receipt state before the change
    /// </summary>
    public string? PreviousState { get; set; }

    /// <summary>
    /// JSON snapshot of receipt state after the change
    /// </summary>
    public string NewState { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// When this audit event can be deleted (Timestamp + 7 years for compliance)
    /// </summary>
    public DateTime RetainUntil { get; set; }
}
