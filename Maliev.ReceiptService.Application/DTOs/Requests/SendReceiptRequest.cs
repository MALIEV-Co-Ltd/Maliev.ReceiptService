using System.ComponentModel.DataAnnotations;

namespace Maliev.ReceiptService.Application.DTOs.Requests;

public class SendReceiptRequest
{
    [Required]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public string Channel { get; set; } = string.Empty;
}
