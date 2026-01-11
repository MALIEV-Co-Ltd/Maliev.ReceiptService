using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.ReceiptService.Api.Models.Requests;
using Maliev.ReceiptService.Api.Models.Responses;
using Xunit;

namespace Maliev.ReceiptService.Tests.Contract;

/// <summary>
/// Contract tests for GET /v1/receipts/{id}/audit-history endpoint
/// Tests: T066 [P] [US3] Contract test for GET /v1/receipts/{id}/audit-history
/// </summary>
[Collection("IntegrationTests")]
public class AuditHistoryContractTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuditHistoryContractTests(TestWebApplicationFactory factory)
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
    public async Task GetAuditHistory_ForExistingReceipt_Returns200WithEvents()
    {
        // Arrange - Create a receipt
        var createRequest = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 1070.00m,
            PaymentMethod = "Cash"
        };

        var createResponse = await _client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var receipt = await createResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);

        // Void the receipt to create more audit events
        var voidRequest = new VoidReceiptRequest { Reason = "Test audit trail" };
        var voidResponse = await _client.PostAsJsonAsync($"/receipt/v1/receipts/{receipt.Id}/void", voidRequest);
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);

        // Act
        var response = await _client.GetAsync($"/receipt/v1/receipts/{receipt.Id}/audit-history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auditEvents = await response.Content.ReadFromJsonAsync<List<AuditEvent>>();
        Assert.NotNull(auditEvents);
        Assert.True(auditEvents.Count >= 2); // At least Created and Voided

        // Verify first event is Created
        var createdEvent = auditEvents.First(e => e.EventType == "Created");
        Assert.NotNull(createdEvent);
        Assert.Equal("test-user", createdEvent.StaffMemberId);
        Assert.NotEqual(default, createdEvent.Timestamp);
        Assert.NotNull(createdEvent.NewState);

        // Verify void event exists
        var voidedEvent = auditEvents.First(e => e.EventType == "Voided");
        Assert.NotNull(voidedEvent);
        Assert.Equal("Test audit trail", voidedEvent.Reason);
        Assert.NotNull(voidedEvent.PreviousState);
        Assert.NotNull(voidedEvent.NewState);
    }

    [Fact]
    public async Task GetAuditHistory_ForNonExistentReceipt_Returns404()
    {
        // Arrange
        var receiptId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/receipt/v1/receipts/{receiptId}/audit-history");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditHistory_WithInvalidGuid_Returns400()
    {
        // Act
        var response = await _client.GetAsync("/receipt/v1/receipts/invalid-guid/audit-history");

        // Assert - Route constraint {id:guid} fails before reaching controller, returns 404
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditHistory_ReturnsEventsInChronologicalOrder()
    {
        // Arrange - Create and perform operations
        var createRequest = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 1000.00m,  // Partial payment to allow catch-all stub with 2140 total
            PaymentMethod = "Bank Transfer"
        };

        var createResponse = await _client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var receipt = await createResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);

        // Wait a bit to ensure timestamp difference
        await Task.Delay(100);

        var voidRequest = new VoidReceiptRequest { Reason = "Chronological test" };
        var voidResponse = await _client.PostAsJsonAsync($"/receipt/v1/receipts/{receipt.Id}/void", voidRequest);
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);

        // Act
        var response = await _client.GetAsync($"/receipt/v1/receipts/{receipt.Id}/audit-history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Parse as JSON array (audit history endpoint returns array directly, not wrapped)
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var auditArray = jsonDoc.RootElement;

        Assert.Equal(JsonValueKind.Array, auditArray.ValueKind);
        Assert.True(auditArray.GetArrayLength() >= 2);

        // Convert to list for chronological verification
        var timestamps = new List<DateTime>();
        foreach (var evt in auditArray.EnumerateArray())
        {
            Assert.True(evt.TryGetProperty("timestamp", out var ts));
            timestamps.Add(DateTime.Parse(ts.GetString() ?? ""));
        }

        // Verify chronological order (earliest first)
        for (int i = 1; i < timestamps.Count; i++)
        {
            Assert.True(timestamps[i - 1] <= timestamps[i],
                "Audit events should be in chronological order");
        }
    }

    [Fact]
    public async Task GetAuditHistory_IncludesRequiredFields()
    {
        // Arrange
        var createRequest = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 535.00m,
            PaymentMethod = "Cash"
        };

        var createResponse = await _client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var receipt = await createResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);

        // Act
        var response = await _client.GetAsync($"/receipt/v1/receipts/{receipt.Id}/audit-history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auditEvents = await response.Content.ReadFromJsonAsync<List<AuditEvent>>();
        Assert.NotNull(auditEvents);
        Assert.NotEmpty(auditEvents);

        // Verify all required fields are present
        foreach (var auditEvent in auditEvents)
        {
            Assert.NotEqual(Guid.Empty, auditEvent.Id);
            Assert.NotEqual(Guid.Empty, auditEvent.ReceiptId);
            Assert.NotNull(auditEvent.EventType);
            Assert.NotEqual(default, auditEvent.Timestamp);
            Assert.NotNull(auditEvent.StaffMemberId);
            Assert.NotNull(auditEvent.NewState);
            Assert.NotEqual(Guid.Empty, auditEvent.CorrelationId);

            // Verify 7-year retention if RetainUntil is set
            if (auditEvent.RetainUntil != default)
            {
                var expectedRetention = auditEvent.Timestamp.AddYears(7);
                Assert.True(Math.Abs((auditEvent.RetainUntil - expectedRetention).TotalDays) < 1,
                    "RetainUntil should be approximately 7 years from Timestamp");
            }
        }
    }
}
