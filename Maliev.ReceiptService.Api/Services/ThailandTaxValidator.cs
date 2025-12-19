using Maliev.ReceiptService.Api.Models.Dtos;
using Maliev.ReceiptService.Api.Models.Requests;

namespace Maliev.ReceiptService.Api.Services;

/// <summary>
/// Thailand-specific tax validator per research.md Decision 7
/// Validates Thai tax compliance requirements for receipts
/// </summary>
public class ThailandTaxValidator : ITaxValidator
{
    private const decimal ThailandVatRate = 7.0m;

    public ValidationResult ValidateReceipt(InvoiceDto invoice, CreateReceiptRequest request)
    {
        var errors = new List<string>();

        // Validate Tax ID (เลขประจำตัวผู้เสียภาษี) - required for Thailand
        if (string.IsNullOrWhiteSpace(invoice.CustomerTaxId))
        {
            errors.Add("Tax ID (เลขประจำตัวผู้เสียภาษี) is required");
        }

        // Validate VAT rate - must be 7% for Thailand
        if (invoice.VatRate != ThailandVatRate)
        {
            errors.Add($"VAT rate must be 7% (got {invoice.VatRate}%)");
        }

        // Validate withholding tax rate if present (must be between 0-100%)
        if (invoice.WithholdingTaxRate.HasValue)
        {
            if (invoice.WithholdingTaxRate.Value < 0 || invoice.WithholdingTaxRate.Value > 100)
            {
                errors.Add("Withholding tax rate must be between 0-100%");
            }
        }

        // Validate withholding tax amount if present (must not exceed total amount)
        if (invoice.WithholdingTaxAmount.HasValue)
        {
            if (invoice.WithholdingTaxAmount.Value < 0)
            {
                errors.Add("Withholding tax amount cannot be negative");
            }
            if (invoice.WithholdingTaxAmount.Value > invoice.TotalAmount)
            {
                errors.Add("Withholding tax amount cannot exceed total amount");
            }
        }

        return errors.Any()
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
