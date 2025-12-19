using Maliev.ReceiptService.Api.Models.Dtos;
using Maliev.ReceiptService.Api.Models.Requests;
using Maliev.ReceiptService.Api.Services;
using Xunit;

namespace Maliev.ReceiptService.Tests.Unit;

public class TaxValidatorTests
{
    [Fact]
    public void ValidateReceipt_WithValidThailandTaxFields_ReturnsSuccess()
    {
        // Arrange
        var validator = CreateThailandTaxValidator();
        var invoice = CreateInvoice(
            taxId: "0105536000000",
            vatRate: 7.0m,
            withholdingTaxRate: null
        );
        var request = CreateReceiptRequest(amount: 1070.00m);

        // Act
        var result = validator.ValidateReceipt(invoice, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateReceipt_WithMissingTaxId_ReturnsFailure()
    {
        // Arrange
        var validator = CreateThailandTaxValidator();
        var invoice = CreateInvoice(
            taxId: null,
            vatRate: 7.0m,
            withholdingTaxRate: null
        );
        var request = CreateReceiptRequest(amount: 1070.00m);

        // Act
        var result = validator.ValidateReceipt(invoice, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Tax ID"));
    }

    [Fact]
    public void ValidateReceipt_WithEmptyTaxId_ReturnsFailure()
    {
        // Arrange
        var validator = CreateThailandTaxValidator();
        var invoice = CreateInvoice(
            taxId: "",
            vatRate: 7.0m,
            withholdingTaxRate: null
        );
        var request = CreateReceiptRequest(amount: 1070.00m);

        // Act
        var result = validator.ValidateReceipt(invoice, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Tax ID"));
    }

    [Fact]
    public void ValidateReceipt_WithIncorrectVatRate_ReturnsFailure()
    {
        // Arrange
        var validator = CreateThailandTaxValidator();
        var invoice = CreateInvoice(
            taxId: "0105536000000",
            vatRate: 10.0m,  // Invalid - should be 7% for Thailand
            withholdingTaxRate: null
        );
        var request = CreateReceiptRequest(amount: 1070.00m);

        // Act
        var result = validator.ValidateReceipt(invoice, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("VAT rate") && e.Contains("7%"));
    }

    [Fact]
    public void ValidateReceipt_WithNegativeWithholdingTaxRate_ReturnsFailure()
    {
        // Arrange
        var validator = CreateThailandTaxValidator();
        var invoice = CreateInvoice(
            taxId: "0105536000000",
            vatRate: 7.0m,
            withholdingTaxRate: -5.0m  // Invalid - negative
        );
        var request = CreateReceiptRequest(amount: 1070.00m);

        // Act
        var result = validator.ValidateReceipt(invoice, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Withholding tax rate") && e.Contains("0-100"));
    }

    [Fact]
    public void ValidateReceipt_WithExcessiveWithholdingTaxRate_ReturnsFailure()
    {
        // Arrange
        var validator = CreateThailandTaxValidator();
        var invoice = CreateInvoice(
            taxId: "0105536000000",
            vatRate: 7.0m,
            withholdingTaxRate: 150.0m  // Invalid - over 100%
        );
        var request = CreateReceiptRequest(amount: 1070.00m);

        // Act
        var result = validator.ValidateReceipt(invoice, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Withholding tax rate") && e.Contains("0-100"));
    }

    [Fact]
    public void ValidateReceipt_WithValidWithholdingTaxRate_ReturnsSuccess()
    {
        // Arrange
        var validator = CreateThailandTaxValidator();
        var invoice = CreateInvoice(
            taxId: "0105536000000",
            vatRate: 7.0m,
            withholdingTaxRate: 3.0m  // Valid withholding tax
        );
        var request = CreateReceiptRequest(amount: 1070.00m);

        // Act
        var result = validator.ValidateReceipt(invoice, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateReceipt_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var validator = CreateThailandTaxValidator();
        var invoice = CreateInvoice(
            taxId: null,  // Missing
            vatRate: 10.0m,  // Wrong
            withholdingTaxRate: -5.0m  // Invalid
        );
        var request = CreateReceiptRequest(amount: 1070.00m);

        // Act
        var result = validator.ValidateReceipt(invoice, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.Errors.Count >= 3);
        Assert.Contains(result.Errors, e => e.Contains("Tax ID"));
        Assert.Contains(result.Errors, e => e.Contains("VAT rate"));
        Assert.Contains(result.Errors, e => e.Contains("Withholding tax rate"));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(3.0)]
    [InlineData(5.0)]
    [InlineData(10.0)]
    [InlineData(15.0)]
    [InlineData(100.0)]
    public void ValidateReceipt_WithValidWithholdingTaxRateRange_ReturnsSuccess(decimal withholdingTaxRate)
    {
        // Arrange
        var validator = CreateThailandTaxValidator();
        var invoice = CreateInvoice(
            taxId: "0105536000000",
            vatRate: 7.0m,
            withholdingTaxRate: withholdingTaxRate
        );
        var request = CreateReceiptRequest(amount: 1070.00m);

        // Act
        var result = validator.ValidateReceipt(invoice, request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateReceipt_WithZeroVatRate_ReturnsFailure()
    {
        // Arrange
        var validator = CreateThailandTaxValidator();
        var invoice = CreateInvoice(
            taxId: "0105536000000",
            vatRate: 0.0m,  // Invalid - Thailand requires 7%
            withholdingTaxRate: null
        );
        var request = CreateReceiptRequest(amount: 1000.00m);

        // Act
        var result = validator.ValidateReceipt(invoice, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("VAT rate") && e.Contains("7%"));
    }

    // Helper methods to create test data structures
    private ITaxValidator CreateThailandTaxValidator()
    {
        return new ThailandTaxValidator();
    }

    private InvoiceDto CreateInvoice(string? taxId, decimal vatRate, decimal? withholdingTaxRate)
    {
        return new InvoiceDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-2025-00001",
            IssueDate = DateTime.UtcNow,
            Status = "Paid",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerTaxId = taxId ?? string.Empty,
            CustomerAddress = "123 Test St",
            Subtotal = 1000.00m,
            TaxAmount = 70.00m,
            WithholdingTaxRate = withholdingTaxRate,
            WithholdingTaxAmount = withholdingTaxRate.HasValue ? 1000.00m * (withholdingTaxRate.Value / 100) : null,
            TotalAmount = 1070.00m,
            Currency = "THB",
            SellerTaxId = "0105536000000",
            VatRate = vatRate,
            WithholdingTaxType = withholdingTaxRate.HasValue ? "WHT3" : "None",
            LineItems = new List<InvoiceLineItemDto>
            {
                new InvoiceLineItemDto
                {
                    Id = Guid.NewGuid(),
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 10,
                    UnitPrice = 100.00m,
                    TaxRate = vatRate,
                    LineTotal = 1070.00m
                }
            },
            TotalPaidAmount = 0,
            RemainingBalance = 1070.00m,
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };
    }

    private CreateReceiptRequest CreateReceiptRequest(decimal amount)
    {
        return new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = amount,
            PaymentMethod = "Bank Transfer"
        };
    }
}
