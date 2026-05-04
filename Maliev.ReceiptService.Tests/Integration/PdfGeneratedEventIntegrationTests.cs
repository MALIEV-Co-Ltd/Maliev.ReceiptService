using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Pdf;
using Maliev.MessagingContracts.Contracts.Receipts;
using Maliev.ReceiptService.Api.Consumers;
using Maliev.ReceiptService.Domain.Entities;
using Maliev.ReceiptService.Domain.Enums;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

[Collection("IntegrationTests")]
public class PdfGeneratedEventIntegrationTests : BaseReceiptIntegrationTest
{
    public PdfGeneratedEventIntegrationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task PdfGeneratedEventConsumer_UpdatesReceiptStatus()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Maliev.ReceiptService.Infrastructure.Data.ReceiptDbContext>();

        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = "TEST-PDF-001",
            Status = ReceiptStatus.PendingPdf,
            IssueDate = DateTime.UtcNow,
            TotalAmount = 1000m,
            CustomerName = "Test Customer",
            Currency = "THB"
        };
        context.Receipts.Add(receipt);
        await context.SaveChangesAsync();

        var publishEndpointMock = new Mock<IPublishEndpoint>();
        var loggerMock = new Mock<ILogger<PdfGeneratedEventConsumer>>();
        var consumer = new PdfGeneratedEventConsumer(context, publishEndpointMock.Object, loggerMock.Object);

        var message = new PdfGenerationCompletedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "PdfGenerationCompletedEvent",
            MessageType: Maliev.MessagingContracts.Contracts.Shared.MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "PdfService",
            ConsumedBy: new[] { "ReceiptService" },
            CorrelationId: Guid.NewGuid(),
            CausationId: Guid.NewGuid(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PdfGenerationCompletedEventPayload(
                RequestId: Guid.NewGuid().ToString(),
                ReferenceId: receipt.Id.ToString(),
                DocumentType: "Receipt",
                StorageUrl: "http://storage/receipt.pdf",
                CompletedAt: DateTimeOffset.UtcNow
            )
        );

        var consumeContextMock = new Mock<ConsumeContext<PdfGenerationCompletedEvent>>();
        consumeContextMock.Setup(c => c.Message).Returns(message);

        // Act
        await consumer.Consume((ConsumeContext<MessagingContracts.Contracts.Pdf.PdfGenerationCompletedEvent>)consumeContextMock.Object);

        // Assert
        var updatedReceipt = await context.Receipts.FindAsync(receipt.Id);
        Assert.Equal(ReceiptStatus.Active, updatedReceipt!.Status);
        Assert.NotNull(updatedReceipt.PdfReferenceId);

        publishEndpointMock.Verify(p => p.Publish(It.IsAny<ReceiptGeneratedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PdfGeneratedEventConsumer_NonReceiptDocument_DoesNotUpdateReceipt()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Maliev.ReceiptService.Infrastructure.Data.ReceiptDbContext>();

        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = "TEST-PDF-QUOTATION-001",
            Status = ReceiptStatus.PendingPdf,
            IssueDate = DateTime.UtcNow,
            TotalAmount = 1000m,
            CustomerName = "Test Customer",
            Currency = "THB"
        };
        context.Receipts.Add(receipt);
        await context.SaveChangesAsync();

        var publishEndpointMock = new Mock<IPublishEndpoint>();
        var loggerMock = new Mock<ILogger<PdfGeneratedEventConsumer>>();
        var consumer = new PdfGeneratedEventConsumer(context, publishEndpointMock.Object, loggerMock.Object);

        var message = new PdfGenerationCompletedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "PdfGenerationCompletedEvent",
            MessageType: Maliev.MessagingContracts.Contracts.Shared.MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "PdfService",
            ConsumedBy: new[] { "InvoiceService", "QuotationService", "ReceiptService" },
            CorrelationId: Guid.NewGuid(),
            CausationId: Guid.NewGuid(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PdfGenerationCompletedEventPayload(
                RequestId: Guid.NewGuid().ToString(),
                ReferenceId: receipt.Id.ToString(),
                DocumentType: "Quotation",
                StorageUrl: "http://storage/quotation.pdf",
                CompletedAt: DateTimeOffset.UtcNow
            )
        );

        var consumeContextMock = new Mock<ConsumeContext<PdfGenerationCompletedEvent>>();
        consumeContextMock.Setup(c => c.Message).Returns(message);

        // Act
        await consumer.Consume((ConsumeContext<MessagingContracts.Contracts.Pdf.PdfGenerationCompletedEvent>)consumeContextMock.Object);

        // Assert
        var unchangedReceipt = await context.Receipts.FindAsync(receipt.Id);
        Assert.Equal(ReceiptStatus.PendingPdf, unchangedReceipt!.Status);
        Assert.Null(unchangedReceipt.PdfReferenceId);

        publishEndpointMock.Verify(p => p.Publish(It.IsAny<ReceiptGeneratedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
