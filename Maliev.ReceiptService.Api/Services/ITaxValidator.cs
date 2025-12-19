using Maliev.ReceiptService.Api.Models.Dtos;
using Maliev.ReceiptService.Api.Models.Requests;

namespace Maliev.ReceiptService.Api.Services;

/// <summary>
/// Tax validation interface per research.md Decision 7
/// Pluggable validator for jurisdiction-specific tax rules
/// </summary>
public interface ITaxValidator
{
    /// <summary>
    /// Validates tax compliance for receipt creation
    /// </summary>
    /// <param name="invoice">Invoice data from Invoice Service</param>
    /// <param name="request">Receipt creation request</param>
    /// <returns>Validation result with errors if invalid</returns>
    ValidationResult ValidateReceipt(InvoiceDto invoice, CreateReceiptRequest request);
}

/// <summary>
/// Validation result for tax validation
/// </summary>
public class ValidationResult
{
    public bool IsSuccess { get; init; }
    public List<string> Errors { get; init; } = new();

    public static ValidationResult Success() => new() { IsSuccess = true };

    public static ValidationResult Failure(List<string> errors) =>
        new() { IsSuccess = false, Errors = errors };
}
