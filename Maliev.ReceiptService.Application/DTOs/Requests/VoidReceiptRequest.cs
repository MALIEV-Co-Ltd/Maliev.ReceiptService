using System.ComponentModel.DataAnnotations;

namespace Maliev.ReceiptService.Application.DTOs.Requests;

public class VoidReceiptRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}
