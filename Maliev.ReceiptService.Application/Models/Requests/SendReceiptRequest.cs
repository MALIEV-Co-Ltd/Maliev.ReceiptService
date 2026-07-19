using System.ComponentModel.DataAnnotations;

namespace Maliev.ReceiptService.Application.Models.Requests;

/// <summary>
/// Request model for sending a receipt to customer
/// </summary>
public class SendReceiptRequest
{
    /// <summary>
    /// Destination (email address, phone number, LINE ID, etc.)
    /// </summary>
    [Required(ErrorMessage = "Destination is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Destination must be between 1 and 200 characters")]
    public required string Destination { get; set; }

    /// <summary>
    /// Delivery channel (Email, SMS, LINE, WhatsApp)
    /// </summary>
    [Required(ErrorMessage = "Channel is required")]
    [RegularExpression("^(Email|SMS|LINE|WhatsApp)$", ErrorMessage = "Channel must be one of: Email, SMS, LINE, WhatsApp")]
    public required string Channel { get; set; }
}
