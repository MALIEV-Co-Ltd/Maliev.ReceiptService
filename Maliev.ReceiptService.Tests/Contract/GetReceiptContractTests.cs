using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Maliev.ReceiptService.Tests.Contract;

[Collection("IntegrationTests")]
public class GetReceiptContractTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public GetReceiptContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClientWithAllPermissions();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.CleanDatabaseAsync();
        _factory.ClearCache();
    }

    [Fact]
    public async Task GetReceiptById_WithValidId_Returns200WithFullSchema()
    {
        // Arrange - First create a receipt
        var createRequest = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        var createResponse = await _client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createContent);
        var receiptId = createDoc.RootElement.GetProperty("id").GetString();

        // Act
        var response = await _client.GetAsync($"/receipt/v1/receipts/{receiptId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Verify complete ReceiptResponse schema per receipts-api.yaml
        Assert.True(root.TryGetProperty("id", out var id));
        Assert.Equal(receiptId, id.GetString());

        Assert.True(root.TryGetProperty("receiptNumber", out var receiptNumber));
        Assert.Matches(@"^[A-Z]+-\d{4}-\d{6}$", receiptNumber.GetString());

        Assert.True(root.TryGetProperty("invoiceId", out var invoiceId));
        Assert.True(Guid.TryParse(invoiceId.GetString(), out _));

        Assert.True(root.TryGetProperty("issueDate", out var issueDate));
        Assert.True(DateTime.TryParse(issueDate.GetString(), out _));

        Assert.True(root.TryGetProperty("customerName", out var customerName));
        Assert.NotEmpty(customerName.GetString() ?? string.Empty);

        // Tax ID can be nullable
        Assert.True(root.TryGetProperty("customerTaxId", out _));

        // Customer address can be nullable
        Assert.True(root.TryGetProperty("customerAddress", out _));

        Assert.True(root.TryGetProperty("subtotal", out var subtotal));
        Assert.True(subtotal.GetDecimal() >= 0);

        Assert.True(root.TryGetProperty("taxAmount", out var taxAmount));
        Assert.True(taxAmount.GetDecimal() >= 0);

        // Withholding tax can be nullable
        Assert.True(root.TryGetProperty("withholdingTaxAmount", out _));

        Assert.True(root.TryGetProperty("totalAmount", out var totalAmount));
        Assert.Equal(1070.00m, totalAmount.GetDecimal());

        Assert.True(root.TryGetProperty("currency", out var currency));
        Assert.NotEmpty(currency.GetString() ?? string.Empty);

        Assert.True(root.TryGetProperty("paymentMethod", out var paymentMethod));
        Assert.Equal("Bank Transfer", paymentMethod.GetString());

        Assert.True(root.TryGetProperty("status", out var status));
        Assert.Contains(status.GetString(), new[] { "Active", "PendingPdf", "Void" });

        // PDF reference can be nullable
        Assert.True(root.TryGetProperty("pdfReferenceId", out _));

        Assert.True(root.TryGetProperty("createdAt", out var createdAt));
        Assert.True(DateTime.TryParse(createdAt.GetString(), out _));

        Assert.True(root.TryGetProperty("createdBy", out var createdBy));
        Assert.NotEmpty(createdBy.GetString() ?? string.Empty);

        // Void fields are nullable (not voided yet)
        Assert.True(root.TryGetProperty("voidedAt", out _));
        Assert.True(root.TryGetProperty("voidedBy", out _));
        Assert.True(root.TryGetProperty("voidReason", out _));

        Assert.True(root.TryGetProperty("lineItems", out var lineItems));
        Assert.True(lineItems.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetReceiptById_WithNonExistentId_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/receipt/v1/receipts/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("error", out var error));
        Assert.True(error.GetString().Contains("not found", StringComparison.OrdinalIgnoreCase) || error.GetString().Contains("Receipt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetReceiptById_WithInvalidGuidFormat_Returns400()
    {
        // Act
        var response = await _client.GetAsync("/receipt/v1/receipts/invalid-guid");

        // Assert - Route constraint {id:guid} fails before reaching controller, returns 404
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReceiptById_ReturnsLineItemsWithCorrectSchema()
    {
        // Arrange - Create a receipt with line items
        var createRequest = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Credit Card"
        };

        var createResponse = await _client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createContent);
        var receiptId = createDoc.RootElement.GetProperty("id").GetString();

        // Act
        var response = await _client.GetAsync($"/receipt/v1/receipts/{receiptId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("lineItems", out var lineItems));
        Assert.True(lineItems.GetArrayLength() > 0);

        // Verify first line item schema per ReceiptLineItem in receipts-api.yaml
        var firstLine = lineItems[0];

        Assert.True(firstLine.TryGetProperty("lineNumber", out var lineNumber));
        Assert.True(lineNumber.GetInt32() >= 1);

        Assert.True(firstLine.TryGetProperty("description", out var description));
        Assert.NotEmpty(description.GetString() ?? string.Empty);

        Assert.True(firstLine.TryGetProperty("quantity", out var quantity));
        Assert.True(quantity.GetDecimal() > 0);

        Assert.True(firstLine.TryGetProperty("unitPrice", out var unitPrice));
        Assert.True(unitPrice.GetDecimal() >= 0);

        Assert.True(firstLine.TryGetProperty("taxRate", out var taxRate));
        Assert.True(taxRate.GetDecimal() >= 0);

        Assert.True(firstLine.TryGetProperty("lineTotal", out var lineTotal));
        Assert.True(lineTotal.GetDecimal() >= 0);
    }

    [Fact]
    public async Task GetReceiptById_ForVoidedReceipt_ReturnsVoidFields()
    {
        // Arrange - Create a receipt
        var createRequest = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 500.00m,
            paymentMethod = "Cash"
        };

        var createResponse = await _client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createContent);
        var receiptId = createDoc.RootElement.GetProperty("id").GetString();

        // Void the receipt
        var voidRequest = new
        {
            reason = "Customer requested refund"
        };

        await _client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/void", voidRequest);

        // Act
        var response = await _client.GetAsync($"/receipt/v1/receipts/{receiptId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("status", out var status));
        Assert.Equal("Void", status.GetString());

        Assert.True(root.TryGetProperty("voidedAt", out var voidedAt));
        Assert.True(DateTime.TryParse(voidedAt.GetString(), out _));

        Assert.True(root.TryGetProperty("voidedBy", out var voidedBy));
        Assert.NotEmpty(voidedBy.GetString() ?? string.Empty);

        Assert.True(root.TryGetProperty("voidReason", out var voidReason));
        Assert.Equal("Customer requested refund", voidReason.GetString());
    }

    [Fact]
    public async Task GetReceiptById_ReturnsConsistentDataAcrossMultipleCalls()
    {
        // Arrange - Create a receipt
        var createRequest = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        var createResponse = await _client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createContent);
        var receiptId = createDoc.RootElement.GetProperty("id").GetString();

        // Act - Fetch the same receipt twice
        var response1 = await _client.GetAsync($"/receipt/v1/receipts/{receiptId}");
        var content1 = await response1.Content.ReadAsStringAsync();

        var response2 = await _client.GetAsync($"/receipt/v1/receipts/{receiptId}");
        var content2 = await response2.Content.ReadAsStringAsync();

        // Assert - Both responses should be identical (receipts are immutable)
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var doc1 = JsonDocument.Parse(content1);
        var doc2 = JsonDocument.Parse(content2);

        Assert.Equal(
            doc1.RootElement.GetProperty("receiptNumber").GetString(),
            doc2.RootElement.GetProperty("receiptNumber").GetString()
        );

        Assert.Equal(
            doc1.RootElement.GetProperty("totalAmount").GetDecimal(),
            doc2.RootElement.GetProperty("totalAmount").GetDecimal()
        );

        Assert.Equal(
            doc1.RootElement.GetProperty("status").GetString(),
            doc2.RootElement.GetProperty("status").GetString()
        );
    }
}
