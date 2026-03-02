using Maliev.ReceiptService.Application.DTOs.Requests;
using Maliev.ReceiptService.Application.DTOs.Responses;

namespace Maliev.ReceiptService.Application.Interfaces;

public interface ITaxValidator
{
    ValidationResult ValidateReceipt(InvoiceDto invoice, CreateReceiptRequest request);
}

public class ValidationResult
{
    public bool IsSuccess { get; private set; }
    public List<string> Errors { get; private set; } = new();

    private ValidationResult() { }

    public static ValidationResult Success() => new() { IsSuccess = true };

    public static ValidationResult Failure(List<string> errors) => new() { IsSuccess = false, Errors = errors };
}
