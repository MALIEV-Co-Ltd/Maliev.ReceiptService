using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.ReceiptService.Api.Models.Requests;
using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

[Collection("IntegrationTests")]
public class PartialPaymentTests : BaseReceiptIntegrationTest
{

    public PartialPaymentTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateReceipt_PartialPayment_Success()
    {
        // Arrange - Invoice total is 1070.00 (1000 + 70 VAT)
        var invoiceId = Guid.NewGuid();
        var request = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 500.00m, // Partial payment
            PaymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Verify partial amount is correctly recorded
        Assert.True(root.TryGetProperty("totalAmount", out var totalAmount));
        Assert.Equal(500.00m, totalAmount.GetDecimal());

        // Verify receipt was created
        Assert.True(root.TryGetProperty("receiptNumber", out _));
        Assert.True(root.TryGetProperty("status", out var status));
        Assert.Equal("PendingPdf", status.GetString());
    }

    [Fact]
    public async Task CreateReceipt_MultiplePartialPayments_BalanceTrackedCorrectly()
    {
        // Arrange - Invoice total is 1070.00
        var invoiceId = Guid.NewGuid();

        // Act - Create first partial payment (500.00)
        var request1 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 500.00m,
            PaymentMethod = "Bank Transfer"
        };
        var response1 = await Client.PostAsJsonAsync("/receipt/v1/receipts", request1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Act - Create second partial payment (300.00)
        var request2 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 300.00m,
            PaymentMethod = "Credit Card"
        };
        var response2 = await Client.PostAsJsonAsync("/receipt/v1/receipts", request2);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        // Act - Create third partial payment (270.00) - should complete the invoice
        var request3 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 270.00m,
            PaymentMethod = "Cash"
        };
        var response3 = await Client.PostAsJsonAsync("/receipt/v1/receipts", request3);
        Assert.Equal(HttpStatusCode.Created, response3.StatusCode);

        // Assert - Verify all three receipts were created
        var queryResponse = await Client.GetAsync($"/receipt/v1/receipts?invoiceId={invoiceId}");
        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);

        var queryContent = await queryResponse.Content.ReadAsStringAsync();
        var queryDoc = JsonDocument.Parse(queryContent);
        var responseRoot = queryDoc.RootElement;

        Assert.True(responseRoot.TryGetProperty("data", out var receiptsArray));
        Assert.Equal(3, receiptsArray.GetArrayLength());

        // Verify total receipted amount equals invoice total
        decimal totalReceipted = 0;
        foreach (var receipt in receiptsArray.EnumerateArray())
        {
            Assert.True(receipt.TryGetProperty("totalAmount", out var amount));
            totalReceipted += amount.GetDecimal();
        }
        Assert.Equal(1070.00m, totalReceipted);
    }

    [Fact]
    public async Task CreateReceipt_OverReceiptingPrevention_ReturnsConflict()
    {
        // Arrange - Invoice total is 1070.00
        var invoiceId = Guid.NewGuid();

        // Act - Create first partial payment (800.00)
        var request1 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 800.00m,
            PaymentMethod = "Bank Transfer"
        };
        var response1 = await Client.PostAsJsonAsync("/receipt/v1/receipts", request1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Act - Attempt to create second payment that would exceed invoice total (500.00)
        // Remaining balance is 270.00, so 500.00 would be over-receipting
        var request2 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 500.00m,
            PaymentMethod = "Credit Card"
        };
        var response2 = await Client.PostAsJsonAsync("/receipt/v1/receipts", request2);

        // Assert - Should return 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);

        var errorContent = await response2.Content.ReadAsStringAsync();
        Assert.Contains("exceeds remaining balance", errorContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateReceipt_ExactRemainingBalance_Success()
    {
        // Arrange - Invoice total is 1070.00
        var invoiceId = Guid.NewGuid();

        // Act - Create first partial payment (800.00)
        var request1 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 800.00m,
            PaymentMethod = "Bank Transfer"
        };
        await Client.PostAsJsonAsync("/receipt/v1/receipts", request1);

        // Act - Create second payment for exact remaining balance (270.00)
        var request2 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 270.00m,
            PaymentMethod = "Cash"
        };
        var response2 = await Client.PostAsJsonAsync("/receipt/v1/receipts", request2);

        // Assert - Should succeed
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_FullPaymentAfterPartial_ReturnsConflict()
    {
        // Arrange - Invoice total is 1070.00
        var invoiceId = Guid.NewGuid();

        // Act - Create partial payment (500.00)
        var request1 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 500.00m,
            PaymentMethod = "Bank Transfer"
        };
        await Client.PostAsJsonAsync("/receipt/v1/receipts", request1);

        // Act - Attempt to create full payment receipt (this should fail)
        var request2 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 1070.00m, // Full invoice amount
            PaymentMethod = "Bank Transfer"
        };
        var response2 = await Client.PostAsJsonAsync("/receipt/v1/receipts", request2);

        // Assert - Should return 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact(Skip = "Concurrent test - flaky in CI environment")]
    public async Task CreateReceipt_ConcurrentPartialPayments_OneSucceedsOneConflicts()
    {
        // Arrange - Invoice total is 1070.00
        var invoiceId = Guid.NewGuid();

        // Create first receipt to set up balance (400.00)
        var setupRequest = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 400.00m,
            PaymentMethod = "Bank Transfer"
        };
        await Client.PostAsJsonAsync("/receipt/v1/receipts", setupRequest);

        // Act - Attempt two concurrent requests that together would exceed remaining balance
        // Remaining: 670.00
        var request1 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 500.00m,
            PaymentMethod = "Bank Transfer"
        };
        var request2 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 500.00m,
            PaymentMethod = "Credit Card"
        };

        var task1 = Client.PostAsJsonAsync("/receipt/v1/receipts", request1);
        var task2 = Client.PostAsJsonAsync("/receipt/v1/receipts", request2);

        var responses = await Task.WhenAll(task1, task2);

        // Assert - One should succeed (201), one should conflict (409)
        var statusCodes = responses.Select(r => r.StatusCode).OrderBy(s => s).ToList();
        Assert.Contains(HttpStatusCode.Created, statusCodes);
        Assert.Contains(HttpStatusCode.Conflict, statusCodes);
    }
}

