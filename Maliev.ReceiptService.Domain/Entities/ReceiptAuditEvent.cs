using Maliev.ReceiptService.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.ReceiptService.Domain.Entities;

public class ReceiptAuditEvent
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    public Guid ReceiptId { get; init; }

    [Required]
    public AuditEventType EventType { get; init; }

    [Required]
    public DateTime Timestamp { get; init; }

    [Required]
    [MaxLength(100)]
    public string StaffMemberId { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Reason { get; init; }

    [Column(TypeName = "text")]
    public string? PreviousState { get; init; }

    [Required]
    [Column(TypeName = "text")]
    public string NewState { get; init; } = string.Empty;

    [Required]
    public Guid CorrelationId { get; init; }

    [Required]
    public DateTime RetainUntil { get; init; }

    [ForeignKey(nameof(ReceiptId))]
    public Receipt Receipt { get; init; } = null!;
}
