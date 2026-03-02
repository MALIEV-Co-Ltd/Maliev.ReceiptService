namespace Maliev.ReceiptService.Application.DTOs.Responses;

public class AuditEvent
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string StaffMemberId { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? PreviousState { get; set; }
    public string NewState { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public DateTime RetainUntil { get; set; }
}
