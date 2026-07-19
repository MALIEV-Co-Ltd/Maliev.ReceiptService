using Maliev.MessagingContracts.Contracts.Pdf;
using Maliev.ReceiptService.Api.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.ReceiptService.Tests.Unit;

/// <summary>
/// Unit tests for PDF generation completion event consumption.
/// </summary>
public sealed class PdfGeneratedEventConsumerTests
{
    /// <summary>
    /// Ensures malformed PDF completion events are ignored before side effects.
    /// </summary>
    [Fact]
    public async Task Consume_WithoutPayload_IsIgnored()
    {
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var consumer = new PdfGeneratedEventConsumer(
            null!,
            publishEndpoint.Object,
            Mock.Of<ILogger<PdfGeneratedEventConsumer>>());

        await consumer.Consume(CreateContext(new PdfGenerationCompletedEvent { Payload = null! }).Object);

        publishEndpoint.Verify(
            endpoint => endpoint.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Mock<ConsumeContext<T>> CreateContext<T>(T message)
        where T : class
    {
        var context = new Mock<ConsumeContext<T>>();
        context.Setup(c => c.Message).Returns(message);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}
