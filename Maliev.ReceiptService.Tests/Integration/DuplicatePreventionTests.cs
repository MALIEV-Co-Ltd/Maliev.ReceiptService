using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

/// <summary>
/// Integration tests for duplicate receipt prevention and over-receipting protection
/// Tests balance tracking and concurrency control per research.md Decision 4
/// </summary>
public class DuplicatePreventionTests : BaseReceiptIntegrationTest
{
    public DuplicatePreventionTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateReceipt_ForFullyPaidInvoice_SecondReceiptRejected()
    {
        // Arrange - Create first receipt for full amount
        var invoiceId = Guid.NewGuid().ToString();
        var firstRequest = new
        {
            invoiceId = invoiceId,
            amount = 1070.00m,  // Full invoice amount
            paymentMethod = "Bank Transfer"
        };

        var firstResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act - Try to create second receipt for same invoice
        var secondRequest = new
        {
            invoiceId = invoiceId,
            amount = 100.00m,
            paymentMethod = "Cash"
        };

        var secondResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", secondRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var content = await secondResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Contains(errorCode.GetString(), new[] { "DUPLICATE_RECEIPT", "INSUFFICIENT_BALANCE" });
    }

    [Fact]
    public async Task CreateReceipt_WithAmountExceedingBalance_Rejected()
    {
        // Arrange - Create partial receipt
        var invoiceId = "660e8400-e29b-41d4-a716-446655440000";
        var firstRequest = new
        {
            invoiceId = invoiceId,
            amount = 500.00m,  // Partial payment (invoice total: 1070)
            paymentMethod = "Cash"
        };

        await Client.PostAsJsonAsync("/receipt/v1/receipts", firstRequest);

        // Act - Try to create receipt for more than remaining balance
        var secondRequest = new
        {
            invoiceId = invoiceId,
            amount = 700.00m,  // Exceeds remaining 570
            paymentMethod = "Bank Transfer"
        };

        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", secondRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Equal("INSUFFICIENT_BALANCE", errorCode.GetString());

        Assert.True(root.TryGetProperty("message", out var message));
        var messageText = message.GetString();
        Assert.Contains("700", messageText);  // Amount attempted
        Assert.Contains("570", messageText);  // Remaining balance
    }

    [Fact]
    public async Task CreateReceipt_ConcurrentRequestsForSameInvoice_OnlyOneSucceeds()
    {
        // Arrange
        var invoiceId = "770e8400-e29b-41d4-a716-446655440000";

        // Act - Create 5 concurrent receipt requests for full amount
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            var request = new
            {
                invoiceId = invoiceId,
                amount = 1070.00m,
                paymentMethod = "Bank Transfer"
            };

            tasks.Add(Client.PostAsJsonAsync("/receipt/v1/receipts", request));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - Only one request should succeed
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(4, conflictCount);
    }

    [Fact]
    public async Task CreateReceipt_MultiplePartialPayments_AcceptedUntilFullyPaid()
    {
        // Arrange
        var invoiceId = "880e8400-e29b-41d4-a716-446655440000";

        // Act - Create 3 partial receipts
        var receipt1 = await CreateReceiptAsync(invoiceId, 300.00m);
        var receipt2 = await CreateReceiptAsync(invoiceId, 400.00m);
        var receipt3 = await CreateReceiptAsync(invoiceId, 370.00m);  // Total: 1070

        // Assert - All three should succeed
        Assert.Equal(HttpStatusCode.Created, receipt1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, receipt2.StatusCode);
        Assert.Equal(HttpStatusCode.Created, receipt3.StatusCode);

        // Try one more - should be rejected
        var receipt4 = await CreateReceiptAsync(invoiceId, 10.00m);
        Assert.Equal(HttpStatusCode.Conflict, receipt4.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_BalanceTracking_UpdatedCorrectlyAfterEachReceipt()
    {
        // Arrange
        var invoiceId = "990e8400-e29b-41d4-a716-446655440000";

        // Act & Assert - Create receipts and track balance
        var receipt1 = await CreateReceiptAsync(invoiceId, 200.00m);
        Assert.Equal(HttpStatusCode.Created, receipt1.StatusCode);
        // Remaining: 870

        var receipt2 = await CreateReceiptAsync(invoiceId, 370.00m);
        Assert.Equal(HttpStatusCode.Created, receipt2.StatusCode);
        // Remaining: 500

        var receipt3 = await CreateReceiptAsync(invoiceId, 500.00m);
        Assert.Equal(HttpStatusCode.Created, receipt3.StatusCode);
        // Remaining: 0

        // This should fail - no balance left
        var receipt4 = await CreateReceiptAsync(invoiceId, 1.00m);
        Assert.Equal(HttpStatusCode.Conflict, receipt4.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_WithConcurrencyConflict_ReturnsAppropriateError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid().ToString();

        // Act - Simulate concurrent updates (optimistic locking conflict)
        var request1 = new
        {
            invoiceId = invoiceId,
            amount = 535.00m,  // Exactly half
            paymentMethod = "Cash"
        };

        var request2 = new
        {
            invoiceId = invoiceId,
            amount = 535.00m,  // Exactly half
            paymentMethod = "Bank Transfer"
        };

        // Send requests concurrently
        var task1 = Client.PostAsJsonAsync("/receipt/v1/receipts", request1);
        var task2 = Client.PostAsJsonAsync("/receipt/v1/receipts", request2);

        var responses = await Task.WhenAll(task1, task2);

        // Assert - One succeeds, one gets conflict
        var hasSuccess = responses.Any(r => r.StatusCode == HttpStatusCode.Created);
        var hasConflict = responses.Any(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.True(hasSuccess);
        Assert.True(hasConflict);

        // Verify conflict response contains appropriate error code
        var conflictResponse = responses.First(r => r.StatusCode == HttpStatusCode.Conflict);
        var content = await conflictResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("errorCode", out var errorCode));
        Assert.Contains(errorCode.GetString(), new[] { "CONCURRENCY_CONFLICT", "INSUFFICIENT_BALANCE" });
    }

    [Fact]
    public async Task CreateReceipt_AfterVoid_BalanceRestored()
    {
        // Arrange - Create receipt
        var invoiceId = "bb0e8400-e29b-41d4-a716-446655440000";
        var createRequest = new
        {
            invoiceId = invoiceId,
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        var createResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createContent);
        var receiptId = createDoc.RootElement.GetProperty("id").GetString();

        // Void the receipt
        var voidRequest = new
        {
            reason = "Payment reversed"
        };

        await Client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/void", voidRequest);

        // Act - Try to create new receipt for same invoice
        var newReceiptRequest = new
        {
            invoiceId = invoiceId,
            amount = 1070.00m,
            paymentMethod = "Cash"
        };

        var newReceiptResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", newReceiptRequest);

        // Assert - Should succeed because balance was restored
        Assert.Equal(HttpStatusCode.Created, newReceiptResponse.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_DuplicateDetection_WorksAcrossMultipleInvoices()
    {
        // Arrange & Act - Create receipts for different invoices
        var invoice1 = "cc0e8400-e29b-41d4-a716-446655440000";
        var invoice2 = "dd0e8400-e29b-41d4-a716-446655440000";
        var invoice3 = "ee0e8400-e29b-41d4-a716-446655440000";

        var receipt1 = await CreateReceiptAsync(invoice1, 1070.00m);
        var receipt2 = await CreateReceiptAsync(invoice2, 1070.00m);
        var receipt3 = await CreateReceiptAsync(invoice3, 1070.00m);

        // Assert - All should succeed (different invoices)
        Assert.Equal(HttpStatusCode.Created, receipt1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, receipt2.StatusCode);
        Assert.Equal(HttpStatusCode.Created, receipt3.StatusCode);

        // Try duplicates for each - all should fail
        var duplicate1 = await CreateReceiptAsync(invoice1, 100.00m);
        var duplicate2 = await CreateReceiptAsync(invoice2, 100.00m);
        var duplicate3 = await CreateReceiptAsync(invoice3, 100.00m);

        Assert.Equal(HttpStatusCode.Conflict, duplicate1.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate2.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate3.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_WithExactRemainingBalance_Succeeds()
    {
        // Arrange - Create partial receipt
        var invoiceId = "ff0e8400-e29b-41d4-a716-446655440000";

        await CreateReceiptAsync(invoiceId, 600.00m);

        // Act - Create receipt for exact remaining balance (470)
        var response = await CreateReceiptAsync(invoiceId, 470.00m);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // No balance left - next attempt should fail
        var thirdAttempt = await CreateReceiptAsync(invoiceId, 1.00m);
        Assert.Equal(HttpStatusCode.Conflict, thirdAttempt.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_DuplicatePrevention_IncludesCorrelationIdInError()
    {
        // Arrange - Create first receipt
        var invoiceId = "ab0e8400-e29b-41d4-a716-446655440000";
        await CreateReceiptAsync(invoiceId, 1070.00m);

        // Act - Try duplicate
        var response = await CreateReceiptAsync(invoiceId, 100.00m);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Verify correlation ID header (FR-030)
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task CreateReceipt_BalanceTracker_CreatedOnFirstReceipt()
    {
        // Arrange & Act
        var invoiceId = "cd0e8400-e29b-41d4-a716-446655440000";
        var response = await CreateReceiptAsync(invoiceId, 500.00m);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify balance tracker exists by creating second partial receipt
        var secondResponse = await CreateReceiptAsync(invoiceId, 570.00m);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        // Third should fail - balance exhausted
        var thirdResponse = await CreateReceiptAsync(invoiceId, 1.00m);
        Assert.Equal(HttpStatusCode.Conflict, thirdResponse.StatusCode);
    }

    // Helper methods
    private async Task<HttpResponseMessage> CreateReceiptAsync(string invoiceId, decimal amount)
    {
        var request = new
        {
            invoiceId = invoiceId,
            amount = amount,
            paymentMethod = "Bank Transfer"
        };

        return await Client.PostAsJsonAsync("/receipt/v1/receipts", request);
    }
}
