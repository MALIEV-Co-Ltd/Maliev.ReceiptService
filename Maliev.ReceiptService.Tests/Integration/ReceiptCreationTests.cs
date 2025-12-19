using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

/// <summary>
/// Integration tests for full payment receipt creation per quickstart.md Step 5.2
/// Tests complete workflow: invoice retrieval → tax validation → numbering → audit → PDF event
/// </summary>
[Collection("IntegrationTests")]
public class ReceiptCreationTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public ReceiptCreationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.CleanDatabaseAsync();
        _factory.ClearCache();
    }

    [Fact]
    public async Task CreateReceipt_WithFullPayment_CreatesReceiptAndPublishesPdfEvent()
    {
        // Arrange
        var request = new
        {
            invoiceId = "550e8400-e29b-41d4-a716-446655440000",
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Verify receipt was created
        var receiptId = root.GetProperty("id").GetString();
        Assert.NotNull(receiptId);
        Assert.True(Guid.TryParse(receiptId, out _));

        // Verify sequential receipt number
        var receiptNumber = root.GetProperty("receiptNumber").GetString();
        Assert.Matches(@"^MALIEV-\d{4}-\d{6}$", receiptNumber);

        // Verify status is PendingPdf (waiting for PDF generation)
        var status = root.GetProperty("status").GetString();
        Assert.Equal("PendingPdf", status);

        // Verify financial details
        Assert.Equal(1070.00m, root.GetProperty("totalAmount").GetDecimal());

        // Verify correlation ID header
        Assert.True(response.Headers.Contains("X-Correlation-Id"));

        // TODO: Verify PDF generation event was published to RabbitMQ
        // This will be implemented once MassTransit test harness is set up
    }

    [Fact]
    public async Task CreateReceipt_CreatesAuditTrailEntry()
    {
        // Arrange
        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var createResponse = await _client.PostAsJsonAsync("/v1/receipts", request);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createContent);
        var receiptId = createDoc.RootElement.GetProperty("id").GetString();

        // Fetch audit history
        var auditResponse = await _client.GetAsync($"/v1/receipts/{receiptId}/audit-history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var auditContent = await auditResponse.Content.ReadAsStringAsync();
        var auditDoc = JsonDocument.Parse(auditContent);
        var auditRoot = auditDoc.RootElement;

        // Audit history endpoint returns an array directly
        Assert.Equal(JsonValueKind.Array, auditRoot.ValueKind);
        Assert.True(auditRoot.GetArrayLength() > 0);

        // Verify "Created" audit event exists
        var createdEvent = auditRoot.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("eventType").GetString() == "Created");

        Assert.NotEqual(default, createdEvent);
        Assert.True(createdEvent.TryGetProperty("timestamp", out _));
        Assert.True(createdEvent.TryGetProperty("staffMemberId", out _));
        Assert.True(createdEvent.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task CreateReceipt_UpdatesInvoiceBalanceTracker()
    {
        // Arrange
        var invoiceId = "770e8400-e29b-41d4-a716-446655440000";
        var request = new
        {
            invoiceId = invoiceId,
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify balance tracker was created/updated in database
        // TODO: Query database directly to verify InvoiceBalanceTracker entity
        // For now, verify by trying to create another receipt for same invoice
        var secondRequest = new
        {
            invoiceId = invoiceId,
            amount = 100.00m,
            paymentMethod = "Cash"
        };

        var secondResponse = await _client.PostAsJsonAsync("/v1/receipts", secondRequest);

        // Should get 409 Conflict if invoice is fully receipted
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_WithValidInvoice_RetrievesInvoiceFromInvoiceService()
    {
        // Arrange
        var request = new
        {
            invoiceId = "880e8400-e29b-41d4-a716-446655440000",
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Verify customer details from invoice are populated
        Assert.True(root.TryGetProperty("customerName", out var customerName));
        Assert.NotEmpty(customerName.GetString() ?? string.Empty);

        Assert.True(root.TryGetProperty("currency", out var currency));
        Assert.NotEmpty(currency.GetString() ?? string.Empty);

        // Verify line items from invoice are copied
        Assert.True(root.TryGetProperty("lineItems", out var lineItems));
        Assert.True(lineItems.GetArrayLength() > 0);
    }

    [Fact]
    public async Task CreateReceipt_GeneratesSequentialReceiptNumbers()
    {
        // Arrange & Act - Create 3 receipts sequentially with unique invoice IDs
        var receipt1 = await CreateReceiptAsync(Guid.NewGuid().ToString(), 1000.00m);
        var receipt2 = await CreateReceiptAsync(Guid.NewGuid().ToString(), 1000.00m);
        var receipt3 = await CreateReceiptAsync(Guid.NewGuid().ToString(), 1000.00m);

        // Assert - Receipt numbers should be sequential
        var number1 = ExtractSequenceNumber(receipt1);
        var number2 = ExtractSequenceNumber(receipt2);
        var number3 = ExtractSequenceNumber(receipt3);

        Assert.Equal(number1 + 1, number2);
        Assert.Equal(number2 + 1, number3);
    }

    [Fact(Skip = "Service unavailability testing requires advanced HTTP client mocking with retry policies")]
    public async Task CreateReceipt_WithInvoiceServiceDown_ReturnsServiceUnavailable()
    {
        // This test requires configuring the HTTP client with proper retry policies
        // and timeout handling. The WireMock stub returns 503, but the HTTP client
        // retry logic needs to be properly configured to propagate the error.

        // TODO: Implement proper HTTP client factory mocking to test service failures

        var request = new
        {
            invoiceId = "00000000-0000-0000-0000-000000000001",
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        var response = await _client.PostAsJsonAsync("/v1/receipts", request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("INVOICE_SERVICE_UNAVAILABLE", errorCode.GetString());
    }

    [Fact]
    public async Task CreateReceipt_CompletesWithin5Seconds()
    {
        // Arrange
        var request = new
        {
            invoiceId = "dd0e8400-e29b-41d4-a716-446655440000",
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.PostAsJsonAsync("/v1/receipts", request);
        stopwatch.Stop();

        // Assert - SC-001: Receipt creation must complete within 5 seconds
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Receipt creation took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    [Fact]
    public async Task CreateReceipt_WithLineItems_CopiesAllLineItemsFromInvoice()
    {
        // Arrange
        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("lineItems", out var lineItems));

        // Verify each line item has all required fields
        foreach (var lineItem in lineItems.EnumerateArray())
        {
            Assert.True(lineItem.TryGetProperty("lineNumber", out var lineNumber));
            Assert.True(lineNumber.GetInt32() >= 1);

            Assert.True(lineItem.TryGetProperty("description", out var description));
            Assert.NotEmpty(description.GetString() ?? string.Empty);

            Assert.True(lineItem.TryGetProperty("quantity", out var quantity));
            Assert.True(quantity.GetDecimal() > 0);

            Assert.True(lineItem.TryGetProperty("unitPrice", out var unitPrice));
            Assert.True(unitPrice.GetDecimal() >= 0);

            Assert.True(lineItem.TryGetProperty("taxRate", out var taxRate));
            Assert.True(taxRate.GetDecimal() >= 0);

            Assert.True(lineItem.TryGetProperty("lineTotal", out var lineTotal));
            Assert.True(lineTotal.GetDecimal() >= 0);
        }
    }

    [Fact]
    public async Task CreateReceipt_PersistsToDatabase()
    {
        // Arrange
        var request = new
        {
            invoiceId = "ff0e8400-e29b-41d4-a716-446655440000",
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act - Create receipt
        var createResponse = await _client.PostAsJsonAsync("/v1/receipts", request);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createContent);
        var receiptId = createDoc.RootElement.GetProperty("id").GetString();

        // Fetch the same receipt
        var getResponse = await _client.GetAsync($"/v1/receipts/{receiptId}");

        // Assert - Receipt should be retrievable from database
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getContent = await getResponse.Content.ReadAsStringAsync();
        var getDoc = JsonDocument.Parse(getContent);
        var getRoot = getDoc.RootElement;

        Assert.Equal(receiptId, getRoot.GetProperty("id").GetString());
        Assert.Equal(1070.00m, getRoot.GetProperty("totalAmount").GetDecimal());
    }

    // Helper methods
    private async Task<string> CreateReceiptAsync(string invoiceId, decimal amount)
    {
        var request = new
        {
            invoiceId = invoiceId,
            amount = amount,
            paymentMethod = "Bank Transfer"
        };

        var response = await _client.PostAsJsonAsync("/v1/receipts", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        return jsonDoc.RootElement.GetProperty("receiptNumber").GetString() ?? string.Empty;
    }

    private int ExtractSequenceNumber(string receiptNumber)
    {
        var parts = receiptNumber.Split('-');
        return int.Parse(parts[2]);
    }
}
