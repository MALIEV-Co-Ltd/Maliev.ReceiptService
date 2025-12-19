using System.ComponentModel.DataAnnotations;

namespace Maliev.ReceiptService.Api.Models.Requests;

/// <summary>
/// Request model for voiding a receipt
/// Task: T067 [P] [US3] Create VoidReceiptRequest DTO
/// </summary>
public class VoidReceiptRequest
{
    /// <summary>
    /// Reason for voiding the receipt (required for audit trail)
    /// </summary>
    [Required(ErrorMessage = "Void reason is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Reason must be between 1 and 500 characters")]
    public required string Reason { get; set; }
}
