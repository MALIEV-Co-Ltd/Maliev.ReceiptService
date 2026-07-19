using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.ReceiptService.Application.Models.Requests;
using Xunit;

namespace Maliev.ReceiptService.Tests.Contract;

[Collection("IntegrationTests")]
public class QueryReceiptsContractTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public QueryReceiptsContractTests(TestWebApplicationFactory factory)
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
    public async Task GetReceipts_FilterByInvoiceId_ReturnsMatchingReceipts()
    {
        // Arrange - Create two receipts for same invoice
        var invoiceId = Guid.Parse("660e8400-e29b-41d4-a716-446655440020");
        var request1 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 500.00m,
            PaymentMethod = "Bank Transfer"
        };
        var request2 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 300.00m,
            PaymentMethod = "Credit Card"
        };

        await _client.PostAsJsonAsync("/receipt/v1/receipts", request1);
        await _client.PostAsJsonAsync("/receipt/v1/receipts", request2);

        // Act
        var response = await _client.GetAsync($"/receipt/v1/receipts?invoiceId={invoiceId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(content).RootElement;

        // Verify paged response structure
        Assert.True(responseDoc.TryGetProperty("data", out var dataArray));
        Assert.True(responseDoc.TryGetProperty("pagination", out _));

        // Verify at least 2 receipts returned in data
        Assert.True(dataArray.GetArrayLength() >= 2);

        // Verify all receipts have the correct invoice ID
        foreach (var receipt in dataArray.EnumerateArray())
        {
            Assert.True(receipt.TryGetProperty("invoiceId", out var returnedInvoiceId));
            Assert.Equal(invoiceId.ToString(), returnedInvoiceId.GetString());

            // Verify receipt has required fields per contracts/receipts-api.yaml
            Assert.True(receipt.TryGetProperty("id", out _));
            Assert.True(receipt.TryGetProperty("receiptNumber", out _));
            Assert.True(receipt.TryGetProperty("status", out _));
            Assert.True(receipt.TryGetProperty("totalAmount", out _));
        }
    }

    [Fact]
    public async Task GetReceipts_FilterByStatus_ReturnsActiveReceipts()
    {
        // Arrange - Create an active receipt
        var invoiceId = Guid.Parse("660e8400-e29b-41d4-a716-446655440021");
        var request = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 1070.00m,
            PaymentMethod = "Bank Transfer"
        };

        await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Act
        var response = await _client.GetAsync("/receipt/v1/receipts?status=Active");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(content).RootElement;

        Assert.True(responseDoc.TryGetProperty("data", out var dataArray));

        // Verify all returned receipts have Active status
        foreach (var receipt in dataArray.EnumerateArray())
        {
            Assert.True(receipt.TryGetProperty("status", out var status));
            Assert.Equal("Active", status.GetString());
        }
    }

    [Fact]
    public async Task GetReceipts_FilterByDateRange_ReturnsReceiptsInRange()
    {
        // Arrange
        var invoiceId = Guid.Parse("660e8400-e29b-41d4-a716-446655440022");
        var request = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 1070.00m,
            PaymentMethod = "Bank Transfer"
        };

        await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Act - Query with date range covering today
        var fromDate = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"/receipt/v1/receipts?fromDate={fromDate}&toDate={toDate}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(content).RootElement;

        Assert.True(responseDoc.TryGetProperty("data", out var dataArray));

        // Verify receipts are returned
        Assert.True(dataArray.GetArrayLength() > 0);

        // Verify all receipts have issueDate within range
        foreach (var receipt in dataArray.EnumerateArray())
        {
            Assert.True(receipt.TryGetProperty("issueDate", out var issueDate));
            var issueDateValue = DateTime.Parse(issueDate.GetString() ?? string.Empty);
            Assert.True(issueDateValue >= DateTime.Parse(fromDate));
            Assert.True(issueDateValue <= DateTime.Parse(toDate).AddDays(1));
        }
    }

    [Fact]
    public async Task GetReceipts_MultipleFilters_ReturnsMatchingReceipts()
    {
        // Arrange
        var invoiceId = Guid.Parse("660e8400-e29b-41d4-a716-446655440023");
        var request = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 1070.00m,
            PaymentMethod = "Bank Transfer"
        };

        await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Act - Query with multiple filters
        var fromDate = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
        var response = await _client.GetAsync(
            $"/receipt/v1/receipts?invoiceId={invoiceId}&status=PendingPdf&fromDate={fromDate}&toDate={toDate}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(content).RootElement;

        Assert.True(responseDoc.TryGetProperty("data", out var dataArray));

        // Verify at least one receipt matches all filters
        Assert.True(dataArray.GetArrayLength() > 0);

        foreach (var receipt in dataArray.EnumerateArray())
        {
            Assert.True(receipt.TryGetProperty("invoiceId", out var returnedInvoiceId));
            Assert.Equal(invoiceId.ToString(), returnedInvoiceId.GetString());

            Assert.True(receipt.TryGetProperty("status", out var status));
            Assert.Equal("PendingPdf", status.GetString());
        }
    }

    [Fact]
    public async Task GetReceipts_NoFilters_ReturnsAllReceipts()
    {
        // Arrange - Ensure at least one receipt exists
        var invoiceId = Guid.Parse("660e8400-e29b-41d4-a716-446655440024");
        var request = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 1070.00m,
            PaymentMethod = "Bank Transfer"
        };

        await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Act
        var response = await _client.GetAsync("/receipt/v1/receipts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(content).RootElement;

        Assert.True(responseDoc.TryGetProperty("data", out var dataArray));

        // Verify at least one receipt exists
        Assert.True(dataArray.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetReceipts_InvalidDateFormat_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/receipt/v1/receipts?fromDate=invalid-date");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReceipts_NonExistentInvoiceId_ReturnsEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/receipt/v1/receipts?invoiceId=00000000-0000-0000-0000-000000000000");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(content).RootElement;

        Assert.True(responseDoc.TryGetProperty("data", out var dataArray));

        // Verify empty array is returned
        Assert.Equal(0, dataArray.GetArrayLength());
    }
}
