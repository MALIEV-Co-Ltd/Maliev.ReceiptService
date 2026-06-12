using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Invoices;
using Maliev.MessagingContracts.Contracts.Receipts;
using Maliev.ReceiptService.Infrastructure.Data;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
            Assert.True(await harness.Consumed.Any<InvoicePaymentReceivedEvent>());
            Assert.False(await harness.Published.Any<Fault<InvoicePaymentReceivedEvent>>());

            await using var scope = Factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ReceiptDbContext>();
            var receipts = await db.Receipts
                .AsNoTracking()
                .Where(receipt => receipt.InvoiceId == invoiceId)
                .ToListAsync();

            var receipt = Assert.Single(receipts);
            Assert.Equal(correlationId, receipt.CorrelationId);
            Assert.Equal(paymentId, receipt.ExternalPaymentId);
            Assert.Equal(2140.00m, receipt.TotalAmount);
            Assert.Equal("PaymentService", receipt.PaymentMethod);

            Assert.True(await harness.Published.Any<ReceiptPdfRequestedEvent>(published =>
                published.Context.Message.Payload.ReceiptId == receipt.Id &&
                published.Context.Message.CorrelationId == correlationId));
        }
        finally
        {
            await harness.Stop();
        }
    }
}
