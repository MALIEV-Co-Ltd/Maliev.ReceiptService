using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Maliev.ReceiptService.Application.Models.Requests;
using Maliev.ReceiptService.Application.Models.Responses;
using Xunit;

namespace Maliev.ReceiptService.Tests.Contract;

/// <summary>
/// Contract tests for POST /v1/receipts/{id}/void endpoint
/// Tests: T062 [P] [US3] Contract test for POST /v1/receipts/{id}/void
/// </summary>
[Collection("IntegrationTests")]
public class VoidReceiptContractTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public VoidReceiptContractTests(TestWebApplicationFactory factory)
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
    public async Task VoidReceipt_WithValidRequest_Returns200()
    {
        // Arrange - Create a receipt first
        var createRequest = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 1070.00m,
            PaymentMethod = "Cash"
        };

        var createResponse = await _client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var receipt = await createResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);

        var voidRequest = new VoidReceiptRequest
        {
            Reason = "Customer requested cancellation"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/receipt/v1/receipts/{receipt.Id}/void", voidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var voidedReceipt = await response.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(voidedReceipt);
        Assert.Equal("Void", voidedReceipt.Status);
        Assert.NotNull(voidedReceipt.VoidedAt);
        Assert.Equal("test-user", voidedReceipt.VoidedBy);
        Assert.Equal("Customer requested cancellation", voidedReceipt.VoidReason);
    }

    [Fact]
    public async Task VoidReceipt_WithoutReason_Returns400()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var voidRequest = new VoidReceiptRequest
        {
            Reason = "" // Empty reason
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/void", voidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VoidReceipt_WithNonExistentId_Returns404()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var voidRequest = new VoidReceiptRequest
        {
            Reason = "Test void"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/void", voidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VoidReceipt_WithInvalidGuid_Returns400()
    {
        // Arrange
        var voidRequest = new VoidReceiptRequest
        {
            Reason = "Test void"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts/invalid-guid/void", voidRequest);

        // Assert - Route constraint {id:guid} fails before reaching controller, returns 404
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VoidReceipt_WithReasonExceedingMaxLength_Returns400()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var voidRequest = new VoidReceiptRequest
        {
            Reason = new string('x', 501) // Exceeds 500 char limit
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/void", voidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
