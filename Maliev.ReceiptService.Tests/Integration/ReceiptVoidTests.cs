using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Maliev.ReceiptService.Data.Data;
using Maliev.ReceiptService.Api.Models.Requests;
using Maliev.ReceiptService.Api.Models.Responses;
using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

/// <summary>
/// Integration tests for receipt void operations
/// Tests: T063 [P] [US3] Integration test for void operation
///        T064 [P] [US3] Integration test for double-void prevention
///        T065 [P] [US3] Integration test for void+recreate correction workflow
/// </summary>
[Collection("IntegrationTests")]
public class ReceiptVoidTests : BaseReceiptIntegrationTest
{

    public ReceiptVoidTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task VoidReceipt_ShouldUpdateStatusAndCreateAuditEvent()
    {
        // Arrange - Create a receipt
        var createRequest = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 1070.00m,
            PaymentMethod = "Bank Transfer"
        };

        var createResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var receipt = await createResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);

        var voidRequest = new VoidReceiptRequest
        {
            Reason = "Customer requested refund"
        };

        // Act
        var voidResponse = await Client.PostAsJsonAsync($"/receipt/v1/receipts/{receipt.Id}/void", voidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);
        var voidedReceipt = await voidResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(voidedReceipt);
        Assert.Equal("Void", voidedReceipt.Status);
        Assert.NotNull(voidedReceipt.VoidedAt);
        Assert.Equal("test-user", voidedReceipt.VoidedBy);
        Assert.Equal("Customer requested refund", voidedReceipt.VoidReason);

        // Verify audit trail created
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceiptDbContext>();
        var auditEvents = await dbContext.ReceiptAuditEvents
            .Where(e => e.ReceiptId == receipt.Id)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        Assert.Equal(2, auditEvents.Count); // Created + Voided
        var voidEvent = auditEvents[1];
        Assert.Equal("Voided", voidEvent.EventType.ToString());
        Assert.Equal("test-user", voidEvent.StaffMemberId);
        Assert.Equal("Customer requested refund", voidEvent.Reason);
        Assert.NotNull(voidEvent.PreviousState); // Should have snapshot before void
        Assert.NotNull(voidEvent.NewState); // Should have snapshot after void
    }

    [Fact]
    public async Task VoidReceipt_ShouldRestoreInvoiceBalance()
    {
        // Arrange - Create a partial receipt
        var invoiceId = Guid.NewGuid();
        var createRequest = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 500.00m,
            PaymentMethod = "Cash"
        };

        var createResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var receipt = await createResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);

        // Verify balance before void
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ReceiptDbContext>();
            // InvoiceBalanceTracker has composite key (InvoiceId, SegmentId)
            // For whole-invoice tracking, SegmentId is Guid.Empty
            var tracker = await dbContext.InvoiceBalanceTrackers.FindAsync(invoiceId, Guid.Empty);
            Assert.NotNull(tracker);
            Assert.Equal(500.00m, tracker.TotalReceiptedAmount);
        }

        var voidRequest = new VoidReceiptRequest
        {
            Reason = "Incorrect amount entered"
        };

        // Act
        var voidResponse = await Client.PostAsJsonAsync($"/receipt/v1/receipts/{receipt.Id}/void", voidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);

        // Verify balance restored
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ReceiptDbContext>();
            // InvoiceBalanceTracker has composite key (InvoiceId, SegmentId)
            var tracker = await dbContext.InvoiceBalanceTrackers.FindAsync(invoiceId, Guid.Empty);
            Assert.NotNull(tracker);
            Assert.Equal(0m, tracker.TotalReceiptedAmount); // Should be restored
            Assert.Equal(tracker.TotalInvoiceAmount, tracker.RemainingBalance);
        }
    }

    [Fact]
    public async Task VoidReceipt_AlreadyVoided_ShouldReturn409()
    {
        // Arrange - Create and void a receipt
        var createRequest = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 1070.00m,
            PaymentMethod = "Credit Card"
        };

        var createResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var receipt = await createResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);

        var voidRequest = new VoidReceiptRequest
        {
            Reason = "First void"
        };

        var firstVoidResponse = await Client.PostAsJsonAsync($"/receipt/v1/receipts/{receipt.Id}/void", voidRequest);
        Assert.Equal(HttpStatusCode.OK, firstVoidResponse.StatusCode);

        // Act - Try to void again
        var secondVoidRequest = new VoidReceiptRequest
        {
            Reason = "Second void attempt"
        };
        var secondVoidResponse = await Client.PostAsJsonAsync($"/receipt/v1/receipts/{receipt.Id}/void", secondVoidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondVoidResponse.StatusCode);
    }

    [Fact]
    public async Task VoidAndRecreate_CorrectionWorkflow_ShouldSucceed()
    {
        // Arrange - Create a receipt with wrong amount
        var invoiceId = Guid.NewGuid();
        var createRequest = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 1000.00m, // Wrong amount
            PaymentMethod = "Bank Transfer"
        };

        var createResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var originalReceipt = await createResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(originalReceipt);

        // Act - Void the incorrect receipt
        var voidRequest = new VoidReceiptRequest
        {
            Reason = "Incorrect amount - should be 1070.00"
        };

        var voidResponse = await Client.PostAsJsonAsync($"/receipt/v1/receipts/{originalReceipt.Id}/void", voidRequest);
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);

        // Create corrected receipt
        var correctedRequest = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 1070.00m, // Correct amount
            PaymentMethod = "Bank Transfer"
        };

        var correctedResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", correctedRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, correctedResponse.StatusCode);
        var correctedReceipt = await correctedResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(correctedReceipt);
        Assert.Equal(1070.00m, correctedReceipt.TotalAmount);
        Assert.NotEqual(originalReceipt.ReceiptNumber, correctedReceipt.ReceiptNumber);

        // Verify both receipts exist in database
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceiptDbContext>();
        var receipts = await dbContext.Receipts
            .Where(r => r.InvoiceId == invoiceId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, receipts.Count);
        Assert.Equal("Void", receipts[0].Status.ToString());
        Assert.Equal("PendingPdf", receipts[1].Status.ToString());
    }

    [Fact]
    public async Task VoidReceipt_ShouldIncludeStaffAttribution()
    {
        // Arrange
        var createRequest = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 535.00m,
            PaymentMethod = "Cash"
        };

        var createResponse = await Client.PostAsJsonAsync("/receipt/v1/receipts", createRequest);
        var receipt = await createResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);

        var voidRequest = new VoidReceiptRequest
        {
            Reason = "Duplicate entry by mistake"
        };

        // Act
        var voidResponse = await Client.PostAsJsonAsync($"/receipt/v1/receipts/{receipt.Id}/void", voidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);
        var voidedReceipt = await voidResponse.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(voidedReceipt);
        Assert.Equal("test-user", voidedReceipt.VoidedBy);
        Assert.NotNull(voidedReceipt.VoidedAt);
        Assert.True(voidedReceipt.VoidedAt <= DateTime.UtcNow);
    }
}

