using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Invoices;
using Maliev.MessagingContracts.Contracts.Receipts;
using Maliev.ReceiptService.Domain.Entities;
using Maliev.ReceiptService.Infrastructure.Data;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMockResponse = WireMock.ResponseBuilders.Response;

namespace Maliev.ReceiptService.Tests.Integration;

/// <summary>
/// Integration coverage for receipt creation from invoice payment events.
/// </summary>
[Collection("IntegrationTests")]
public class InvoicePaymentReceivedEventConsumerTests : BaseReceiptIntegrationTest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvoicePaymentReceivedEventConsumerTests"/> class.
    /// </summary>
    public InvoicePaymentReceivedEventConsumerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task InvoicePaymentReceivedEvent_CreatesReceiptAndPublishesPdfRequestIdempotently()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var harness = Factory.Services.GetRequiredService<ITestHarness>();
        StubInvoice(invoiceId);
        StubPayment(paymentId);

        await harness.Start();

        try
        {
            var paymentEvent = new InvoicePaymentReceivedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(InvoicePaymentReceivedEvent),
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "InvoiceService",
                ConsumedBy: ["ReceiptService"],
                CorrelationId: correlationId,
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new InvoicePaymentReceivedEventPayload(
                    InvoiceId: invoiceId,
                    InvoiceNumber: "INV-PAID-001",
                    PaymentId: paymentId,
                    AllocatedAmount: 2140.00,
                    Currency: "THB",
                    RemainingBalance: 0,
                    AllocatedAt: DateTimeOffset.UtcNow,
                    AllocatedBy: Guid.Empty));

            // Act
            await harness.Bus.Publish(paymentEvent);
            await harness.Bus.Publish(paymentEvent);
            await harness.Bus.Publish(paymentEvent with
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid()
            });

            // Assert
            var receipt = await WaitForReceiptAsync(invoiceId);
            Assert.Equal(correlationId, receipt.CorrelationId);
            Assert.Equal(paymentId, receipt.ExternalPaymentId);
            Assert.Equal(2140.00m, receipt.TotalAmount);
            Assert.Equal("omise", receipt.PaymentMethod);

            var pdfRequested = await WaitForAsync(() => harness.Published.Any<ReceiptPdfRequestedEvent>(published =>
                published.Context.Message.Payload.ReceiptId == receipt.Id &&
                published.Context.Message.Payload.FinancialDetails.PaymentMethod == "omise" &&
                published.Context.Message.CorrelationId == correlationId));
            Assert.True(pdfRequested);
            Assert.False(await harness.Published.Any<Fault<InvoicePaymentReceivedEvent>>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    private async Task<Receipt> WaitForReceiptAsync(Guid invoiceId)
    {
        var receipt = await WaitForAsync(async () =>
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ReceiptDbContext>();
            var receipts = await db.Receipts
                .AsNoTracking()
                .Where(candidate => candidate.InvoiceId == invoiceId)
                .ToListAsync();

            return receipts.Count == 1 ? receipts[0] : null;
        });

        Assert.NotNull(receipt);
        return receipt!;
    }

    private static async Task<T?> WaitForAsync<T>(Func<Task<T?>> action)
        where T : class
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = await action();
            if (result is not null)
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return null;
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> action)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await action())
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return false;
    }

    private void StubInvoice(Guid invoiceId)
    {
        Factory.InvoiceServiceMock
            .Given(Request.Create().WithPath($"/invoice/v1/invoices/{invoiceId}").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = invoiceId,
                    invoiceNumber = "INV-PAID-001",
                    customerId = Guid.NewGuid(),
                    customerName = "Payment Event Customer",
                    customerTaxId = "1234567890123",
                    customerAddress = "123 Test Street, Bangkok 10100",
                    sellerTaxId = "9876543210987",
                    currency = "THB",
                    subtotal = 2000.00m,
                    vatRate = 7.0m,
                    taxAmount = 140.00m,
                    withholdingTaxRate = 0.0m,
                    withholdingTaxAmount = 0.0m,
                    withholdingTaxType = "None",
                    totalAmount = 2140.00m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    totalPaidAmount = 0m,
                    remainingBalance = 2140.00m,
                    paymentStatus = "Unpaid",
                    createdAt = DateTime.UtcNow,
                    createdBy = "InvoiceService",
                    lineItems = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 1,
                            description = "Payment event line item",
                            quantity = 20m,
                            unitPrice = 100.00m,
                            taxRate = 7.0m,
                            lineTotal = 2140.00m
                        }
                    }
                })));
    }

    private void StubPayment(Guid paymentId)
    {
        Factory.InvoiceServiceMock
            .Given(Request.Create().WithPath($"/invoice/v1/payments/{paymentId}").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = paymentId,
                    paymentAmount = 2140.00m,
                    paymentDate = DateTime.UtcNow,
                    paymentMethod = "omise",
                    referenceNumber = "ORD-PAID-001",
                    notes = "Paid through Omise test mode",
                    recordedBy = "PaymentService",
                    createdAt = DateTime.UtcNow
                })));
    }
}
