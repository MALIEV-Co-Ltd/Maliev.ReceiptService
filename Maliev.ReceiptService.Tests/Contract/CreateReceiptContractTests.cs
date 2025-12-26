using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Maliev.ReceiptService.Tests.Contract;

[Collection("IntegrationTests")]
public class CreateReceiptContractTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public CreateReceiptContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        // Create authenticated client with all receipt permissions
        var token = factory.CreateTestJwtToken(
            userId: "test-user",
            roles: new[] { "admin" },
            permissions: new[]
            {
                "receipt.receipts.create",
                "receipt.receipts.read",
                "receipt.receipts.update",
                "receipt.receipts.void",
                "receipt.receipts.query"
            });
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean database after each test to prevent duplicate receipt numbers
        await _factory.CleanDatabaseAsync();
        _factory.ClearCache();
    }

    [Fact]
    public async Task PostReceipts_WithValidFullPayment_Returns201WithValidSchema()
    {
        // Arrange
        var testInvoiceId = Guid.NewGuid().ToString();
        var request = new
        {
            invoiceId = testInvoiceId,
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify response schema per receipts-api.yaml
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Required fields per ReceiptResponse schema
        Assert.True(root.TryGetProperty("id", out var id));
        Assert.True(Guid.TryParse(id.GetString(), out _));

        Assert.True(root.TryGetProperty("receiptNumber", out var receiptNumber));
        Assert.Matches(@"^[A-Z]+-\d{4}-\d{6}$", receiptNumber.GetString());

        Assert.True(root.TryGetProperty("invoiceId", out var invoiceId));
        Assert.Equal(testInvoiceId, invoiceId.GetString());

        Assert.True(root.TryGetProperty("issueDate", out var issueDate));
        Assert.True(DateTime.TryParse(issueDate.GetString(), out _));

        Assert.True(root.TryGetProperty("customerName", out _));
        Assert.True(root.TryGetProperty("subtotal", out _));
        Assert.True(root.TryGetProperty("taxAmount", out _));
        Assert.True(root.TryGetProperty("totalAmount", out var totalAmount));
        Assert.Equal(1070.00m, totalAmount.GetDecimal());

        Assert.True(root.TryGetProperty("currency", out _));
        Assert.True(root.TryGetProperty("paymentMethod", out var paymentMethod));
        Assert.Equal("Bank Transfer", paymentMethod.GetString());

        Assert.True(root.TryGetProperty("status", out var status));
        Assert.Contains(status.GetString(), new[] { "Active", "PendingPdf", "Void" });

        Assert.True(root.TryGetProperty("createdBy", out _));
        Assert.True(root.TryGetProperty("lineItems", out var lineItems));
        Assert.True(lineItems.GetArrayLength() > 0);

        // Verify X-Correlation-Id header
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task PostReceipts_WithPartialPayment_Returns201()
    {
        // Arrange
        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 500.00m,
            paymentMethod = "Cash"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostReceipts_WithInvalidAmount_Returns400()
    {
        // Arrange
        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 0.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // ASP.NET Core validation returns ValidationProblemDetails format
        Assert.True(root.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Amount", out _));
    }

    [Fact]
    public async Task PostReceipts_WithMissingInvoiceId_Returns404()
    {
        // Arrange - Missing invoiceId defaults to Guid.Empty (00000000...)
        var request = new
        {
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert - Invoice not found returns 404
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostReceipts_WithNonExistentInvoice_Returns404()
    {
        // Arrange
        var request = new
        {
            invoiceId = "00000000-0000-0000-0000-000000000000",
            amount = 100.00m,
            paymentMethod = "Cash"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("INVOICE_NOT_FOUND", errorCode.GetString());
    }

    [Fact]
    public async Task PostReceipts_WithTaxValidationFailure_Returns400()
    {
        // Arrange - Invoice with missing tax fields
        var request = new
        {
            invoiceId = "11111111-1111-1111-1111-111111111111",
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("TAX_VALIDATION_FAILED", errorCode.GetString());
    }

    [Fact]
    public async Task PostReceipts_WhenInvoiceFullyReceipted_Returns409()
    {
        // Arrange - First create a full receipt using valid invoice
        var testInvoiceId = Guid.NewGuid().ToString();
        var firstRequest = new
        {
            invoiceId = testInvoiceId,
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        await _client.PostAsJsonAsync("/receipt/v1/receipts", firstRequest);

        // Act - Try to create another receipt for same invoice
        var secondRequest = new
        {
            invoiceId = testInvoiceId,
            amount = 100.00m,
            paymentMethod = "Cash"
        };

        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", secondRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Contains(errorCode.GetString(), new[] { "DUPLICATE_RECEIPT", "INSUFFICIENT_BALANCE" });
    }

    [Fact]
    public async Task PostReceipts_WithExcessiveAmount_Returns409()
    {
        // Arrange - Use valid invoice and excessive amount
        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 999999.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("INSUFFICIENT_BALANCE", errorCode.GetString());
    }

    [Fact]
    public async Task PostReceipts_ResponseIncludesLineItems()
    {
        // Arrange - Use standard valid invoice
        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("lineItems", out var lineItems));
        Assert.True(lineItems.GetArrayLength() > 0);

        var firstLine = lineItems[0];
        Assert.True(firstLine.TryGetProperty("lineNumber", out var lineNumber));
        Assert.True(lineNumber.GetInt32() >= 1);

        Assert.True(firstLine.TryGetProperty("description", out _));
        Assert.True(firstLine.TryGetProperty("quantity", out _));
        Assert.True(firstLine.TryGetProperty("unitPrice", out _));
        Assert.True(firstLine.TryGetProperty("taxRate", out _));
        Assert.True(firstLine.TryGetProperty("lineTotal", out _));
    }
}

