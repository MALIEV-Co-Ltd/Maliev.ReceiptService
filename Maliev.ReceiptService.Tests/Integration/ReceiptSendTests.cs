using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

[Collection("IntegrationTests")]
public class ReceiptSendTests : BaseReceiptIntegrationTest
{
    public ReceiptSendTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SendReceipt_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };
        var createResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createContent);
        var receiptId = Guid.Parse(createDoc.RootElement.GetProperty("id").GetString()!);

        // Simulate PDF generation to make receipt Active
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Maliev.ReceiptService.Infrastructure.Data.ReceiptDbContext>();
            var receipt = await context.Receipts.FindAsync(receiptId);
            receipt!.Status = Maliev.ReceiptService.Domain.Enums.ReceiptStatus.Active;
            receipt.PdfReferenceId = Guid.NewGuid();
            await context.SaveChangesAsync();
        }

        var sendRequest = new
        {
            destination = "test@example.com",
            channel = "Email"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/send", sendRequest);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SendReceipt_ReceiptNotFound_ReturnsNotFound()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var sendRequest = new
        {
            destination = "test@example.com",
            channel = "Email"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/send", sendRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendReceipt_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange - create receipt but leave it in PendingPdf status
        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };
        var createResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createContent);
        var receiptId = Guid.Parse(createDoc.RootElement.GetProperty("id").GetString()!);

        var sendRequest = new
        {
            destination = "test@example.com",
            channel = "Email"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/send", sendRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendReceipt_WithoutPdf_ReturnsBadRequest()
    {
        // Arrange - create receipt and set status to Active but no PDF
        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };
        var createResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createDoc = JsonDocument.Parse(createContent);
        var receiptId = Guid.Parse(createDoc.RootElement.GetProperty("id").GetString()!);

        // Set to Active but no PDF
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Maliev.ReceiptService.Infrastructure.Data.ReceiptDbContext>();
            var receipt = await context.Receipts.FindAsync(receiptId);
            receipt!.Status = Maliev.ReceiptService.Domain.Enums.ReceiptStatus.Active;
            // No PdfReferenceId - receipt has no PDF
            await context.SaveChangesAsync();
        }

        var sendRequest = new
        {
            destination = "test@example.com",
            channel = "Email"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/send", sendRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
