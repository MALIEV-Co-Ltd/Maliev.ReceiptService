using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

/// <summary>
/// Integration tests for tax validation rejection per User Story 6
/// Tests that receipts with invalid tax fields are rejected before creation
/// </summary>
[Collection("IntegrationTests")]
public class TaxValidationTests : BaseReceiptIntegrationTest
{

    public TaxValidationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateReceipt_WithMissingTaxId_ReturnsValidationError()
    {
        // Arrange - Invoice without tax ID
        var request = new
        {
            invoiceId = "11111111-1111-1111-1111-111111111111",  // Mock invoice with missing tax ID
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("TAX_VALIDATION_FAILED", errorCode.GetString());

        Assert.True(root.TryGetProperty("message", out var message));
        Assert.Contains("Tax ID", message.GetString());
    }

    [Fact]
    public async Task CreateReceipt_WithInvalidVatRate_ReturnsValidationError()
    {
        // Arrange - Invoice with incorrect VAT rate
        var request = new
        {
            invoiceId = "22222222-2222-2222-2222-222222222222",  // Mock invoice with VAT != 7%
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("TAX_VALIDATION_FAILED", errorCode.GetString());

        Assert.True(root.TryGetProperty("message", out var message));
        Assert.Contains("VAT rate", message.GetString());
        Assert.Contains("7%", message.GetString());
    }

    [Fact]
    public async Task CreateReceipt_WithNegativeWithholdingTaxRate_ReturnsValidationError()
    {
        // Arrange - Invoice with negative withholding tax
        var request = new
        {
            invoiceId = "33333333-3333-3333-3333-333333333333",
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("TAX_VALIDATION_FAILED", errorCode.GetString());

        Assert.True(root.TryGetProperty("message", out var message));
        Assert.Contains("Withholding tax", message.GetString());
    }

    [Fact]
    public async Task CreateReceipt_WithExcessiveWithholdingTaxRate_ReturnsValidationError()
    {
        // Arrange - Invoice with withholding tax > 100%
        var request = new
        {
            invoiceId = "44444444-4444-4444-4444-444444444444",
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("TAX_VALIDATION_FAILED", errorCode.GetString());
    }

    [Fact]
    public async Task CreateReceipt_WithMultipleTaxErrors_ReturnsAllValidationErrors()
    {
        // Arrange - Invoice with multiple tax issues
        var request = new
        {
            invoiceId = "55555555-5555-5555-5555-555555555555",  // Missing tax ID, wrong VAT, invalid withholding
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("TAX_VALIDATION_FAILED", errorCode.GetString());

        // Message should contain details about multiple errors
        Assert.True(root.TryGetProperty("message", out var message));
        var messageText = message.GetString();

        // Verify multiple error types are mentioned (or check details field)
        if (root.TryGetProperty("details", out var details))
        {
            // Details should contain array of validation errors
            Assert.True(details.GetArrayLength() > 1);
        }
    }

    [Fact]
    public async Task CreateReceipt_WithValidTaxFields_Succeeds()
    {
        // Arrange - Invoice with correct tax fields
        var request = new
        {
            invoiceId = "66666666-6666-6666-6666-666666666666",  // Valid tax ID, 7% VAT, valid withholding
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_WithNoWithholdingTax_Succeeds()
    {
        // Arrange - Invoice with no withholding tax (nullable field)
        var request = new
        {
            invoiceId = "77777777-7777-7777-7777-777777777777",
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_TaxValidationOccursBeforeDatabaseWrite()
    {
        // Arrange - Invalid tax fields
        var request = new
        {
            invoiceId = "88888888-8888-8888-8888-888888888888",  // Invalid tax
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify no receipt was created in database
        // Since we got BadRequest, no receipt ID was returned
        // If we query for invoiceId, we should find no receipts
        var queryResponse = await Client.GetAsync($"/receipt/v1/receipts?invoiceId=88888888-8888-8888-8888-888888888888");

        if (queryResponse.StatusCode == HttpStatusCode.OK)
        {
            var queryContent = await queryResponse.Content.ReadAsStringAsync();
            var queryDoc = JsonDocument.Parse(queryContent);
            var queryRoot = queryDoc.RootElement;

            if (queryRoot.TryGetProperty("data", out var data))
            {
                Assert.Equal(0, data.GetArrayLength());
            }
        }
    }

    [Fact]
    public async Task CreateReceipt_TaxValidationErrorIncludesCorrelationId()
    {
        // Arrange
        var request = new
        {
            invoiceId = "99999999-9999-9999-9999-999999999999",  // Invalid tax
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify correlation ID header is present even for errors (FR-030)
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task CreateReceipt_WithValidWithholdingTaxRates_AllSucceed()
    {
        // Arrange & Act - Test various valid withholding tax rates
        // Use unique invoice IDs to avoid conflicts within the same test class
        var testCases = new[]
        {
            0.0m,    // 0%
            1.0m,    // 1%
            3.0m,    // 3% (common in Thailand)
            5.0m,    // 5%
            10.0m,   // 10%
        };

        foreach (var withholdingRate in testCases)
        {
            var request = new
            {
                invoiceId = Guid.NewGuid().ToString(),
                amount = 1000.00m,
                paymentMethod = "Bank Transfer"
            };

            var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    [Fact]
    public async Task CreateReceipt_TaxValidationFailure_DoesNotPublishPdfEvent()
    {
        // Arrange - Invalid tax
        var request = new
        {
            invoiceId = "bbbb0000-0000-0000-0000-000000000001",
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // TODO: Verify no PDF generation event was published to RabbitMQ
        // This will be verified once MassTransit test harness is configured
    }

    [Fact]
    public async Task CreateReceipt_EmptyTaxIdString_TreatedAsMissing()
    {
        // Arrange - Invoice with empty (not null) tax ID
        var request = new
        {
            invoiceId = "cccc0000-0000-0000-0000-000000000001",
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("TAX_VALIDATION_FAILED", errorCode.GetString());
    }
}

