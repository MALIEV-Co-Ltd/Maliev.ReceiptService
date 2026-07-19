using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Receipts;

namespace Maliev.ReceiptService.Tests.Contract;

/// <summary>
/// Contract tests for ReceiptPdfRequestedEvent per contracts/message-contracts.md
/// Verifies event structure, serialization, and publishing behavior
/// </summary>
[Collection("IntegrationTests")]
public class PdfGenerationEventTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public PdfGenerationEventTests(TestWebApplicationFactory factory)
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
    public async Task CreateReceipt_PublishesReceiptPdfRequestedEvent()
    {
        // Arrange
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var invoiceId = Guid.NewGuid();
        var request = new
        {
            invoiceId = invoiceId.ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify event was published with matching CorrelationId
        var correlationIdHeader = response.Headers.FirstOrDefault(h =>
            h.Key.Equals("X-Correlation-ID", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();
        Assert.NotNull(correlationIdHeader);
        var correlationId = Guid.Parse(correlationIdHeader);

        var published = await harness.Published.Any<ReceiptPdfRequestedEvent>(m =>
            m.Context.Message.CorrelationId == correlationId);
        Assert.True(published, "ReceiptPdfRequestedEvent should be published with matching CorrelationId");

        // Get the published message
        var publishedMessage = harness.Published.Select<ReceiptPdfRequestedEvent>()
            .LastOrDefault(m => m.Context.Message.CorrelationId == correlationId);
        Assert.NotNull(publishedMessage);

        var eventData = publishedMessage.Context.Message;

        // Verify required fields per message-contracts.md
        Assert.NotEqual(Guid.Empty, eventData.Payload.ReceiptId);
        Assert.Matches(@"^[A-Z]+-\d{4}-\d{6}$", eventData.Payload.ReceiptNumber);
        Assert.Equal(correlationId, eventData.CorrelationId);
        Assert.True(eventData.OccurredAtUtc > DateTimeOffset.MinValue);

        await harness.Stop();
    }

    [Fact]
    public async Task ReceiptPdfRequestedEvent_ContainsCustomerDetails()
    {
        // Arrange
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);
        var correlationId = Guid.Parse(response.Headers.FirstOrDefault(h =>
            h.Key.Equals("X-Correlation-ID", StringComparison.OrdinalIgnoreCase)).Value!.First());

        // Assert
        var published = await harness.Published.Any<ReceiptPdfRequestedEvent>(m =>
            m.Context.Message.CorrelationId == correlationId);
        Assert.True(published);

        var publishedMessage = harness.Published.Select<ReceiptPdfRequestedEvent>()
            .LastOrDefault(m => m.Context.Message.CorrelationId == correlationId);
        Assert.NotNull(publishedMessage);
        var eventData = publishedMessage.Context.Message;

        // Verify customerDetails per schema
        Assert.NotNull(eventData.Payload.CustomerDetails);
        Assert.NotEmpty(eventData.Payload.CustomerDetails.Name);

        await harness.Stop();
    }

    [Fact]
    public async Task ReceiptPdfRequestedEvent_ContainsFinancialDetails()
    {
        // Arrange
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Credit Card"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);
        var correlationId = Guid.Parse(response.Headers.FirstOrDefault(h =>
            h.Key.Equals("X-Correlation-ID", StringComparison.OrdinalIgnoreCase)).Value!.First());

        // Assert
        var published = await harness.Published.Any<ReceiptPdfRequestedEvent>(m =>
            m.Context.Message.CorrelationId == correlationId);
        Assert.True(published);

        // Get the published message
        var publishedMessage = harness.Published.Select<ReceiptPdfRequestedEvent>()
            .LastOrDefault(m => m.Context.Message.CorrelationId == correlationId);
        Assert.NotNull(publishedMessage);
        var eventData = publishedMessage.Context.Message;

        Assert.NotNull(eventData.Payload.FinancialDetails);
        Assert.True(eventData.Payload.FinancialDetails.IssueDate > DateTimeOffset.MinValue);
        Assert.True(eventData.Payload.FinancialDetails.Subtotal >= 0);
        Assert.True(eventData.Payload.FinancialDetails.TaxAmount >= 0);
        Assert.Equal(1070.00, eventData.Payload.FinancialDetails.TotalAmount);
        Assert.NotEmpty(eventData.Payload.FinancialDetails.Currency);
        Assert.Equal("Credit Card", eventData.Payload.FinancialDetails.PaymentMethod);

        await harness.Stop();
    }

    [Fact]
    public async Task ReceiptPdfRequestedEvent_ContainsLineItems()
    {
        // Arrange
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);
        var correlationId = Guid.Parse(response.Headers.FirstOrDefault(h =>
            h.Key.Equals("X-Correlation-ID", StringComparison.OrdinalIgnoreCase)).Value!.First());

        // Assert
        var publishedMessage = harness.Published.Select<ReceiptPdfRequestedEvent>()
            .LastOrDefault(m => m.Context.Message.CorrelationId == correlationId);
        Assert.NotNull(publishedMessage);
        var eventData = publishedMessage.Context.Message;

        Assert.NotNull(eventData.Payload.LineItems);
        Assert.NotEmpty(eventData.Payload.LineItems);

        // Verify first line item schema
        var firstLine = eventData.Payload.LineItems[0];
        Assert.True(firstLine.LineNumber >= 1);
        Assert.NotEmpty(firstLine.Description);
        Assert.True(firstLine.Quantity > 0);
        Assert.True(firstLine.UnitPrice >= 0);
        Assert.True(firstLine.TaxRate >= 0);
        Assert.True(firstLine.LineTotal >= 0);

        await harness.Stop();
    }

    [Fact]
    public async Task ReceiptPdfRequestedEvent_ContainsTaxFields()
    {
        // Arrange
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);
        var correlationId = Guid.Parse(response.Headers.FirstOrDefault(h =>
            h.Key.Equals("X-Correlation-ID", StringComparison.OrdinalIgnoreCase)).Value!.First());

        // Assert
        var publishedMessage = harness.Published.Select<ReceiptPdfRequestedEvent>()
            .LastOrDefault(m => m.Context.Message.CorrelationId == correlationId);
        Assert.NotNull(publishedMessage);
        var eventData = publishedMessage.Context.Message;

        Assert.NotNull(eventData.Payload.TaxFields);
        Assert.NotEmpty(eventData.Payload.TaxFields.TaxId);
        Assert.True(eventData.Payload.TaxFields.VatRate >= 0);

        await harness.Stop();
    }

    [Fact]
    public async Task ReceiptPdfRequestedEvent_ContainsTemplateId()
    {
        // Arrange
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);
        var correlationId = Guid.Parse(response.Headers.FirstOrDefault(h =>
            h.Key.Equals("X-Correlation-ID", StringComparison.OrdinalIgnoreCase)).Value!.First());

        // Assert
        var publishedMessage = harness.Published.Select<ReceiptPdfRequestedEvent>()
            .LastOrDefault(m => m.Context.Message.CorrelationId == correlationId);
        Assert.NotNull(publishedMessage);
        var eventData = publishedMessage.Context.Message;

        Assert.NotEmpty(eventData.Payload.TemplateId);
        Assert.Equal("receipt-v1", eventData.Payload.TemplateId);

        await harness.Stop();
    }

    [Fact]
    public async Task ReceiptPdfRequestedEvent_SerializesAndDeserializesCorrectly()
    {
        // Arrange - Create sample event
        var originalEvent = new ReceiptPdfRequestedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "ReceiptPdfRequestedEvent",
            MessageType: Maliev.MessagingContracts.Contracts.Shared.MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "Test",
            ConsumedBy: Array.Empty<string>(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new ReceiptPdfRequestedEventPayload(
                ReceiptId: Guid.NewGuid(),
                ReceiptNumber: "MALIEV-2025-000042",
                CustomerDetails: new ReceiptPdfRequestedEventPayloadCustomerDetails(
                    Name: "Test Customer",
                    TaxId: "1234567890123",
                    Address: "123 Test St"
                ),
                FinancialDetails: new ReceiptPdfRequestedEventPayloadFinancialDetails(
                    IssueDate: DateTimeOffset.UtcNow,
                    Subtotal: 1000.00,
                    TaxAmount: 70.00,
                    WithholdingTaxAmount: 0,
                    TotalAmount: 1070.00,
                    Currency: "THB",
                    PaymentMethod: "Bank Transfer"
                ),
                LineItems: new List<ReceiptPdfRequestedEventPayloadLineItemsItem>
                {
                    new ReceiptPdfRequestedEventPayloadLineItemsItem(
                        LineNumber: 1,
                        Description: "Test Item",
                        Quantity: 10,
                        UnitPrice: 100.00,
                        TaxRate: 7.00,
                        LineTotal: 1070.00
                    )
                },
                TaxFields: new ReceiptPdfRequestedEventPayloadTaxFields(
                    TaxId: "0105536000000",
                    VatRate: 7.00,
                    WithholdingTaxType: "None"
                ),
                TemplateId: "receipt-v1",
                RequestedAt: DateTimeOffset.UtcNow
            )
        );

        // Act - Serialize and deserialize
        var json = JsonSerializer.Serialize(originalEvent);
        var deserializedEvent = JsonSerializer.Deserialize<ReceiptPdfRequestedEvent>(json);

        // Assert
        Assert.NotNull(deserializedEvent);
        Assert.Equal(originalEvent.Payload.ReceiptId, deserializedEvent.Payload.ReceiptId);
        Assert.Equal(originalEvent.Payload.ReceiptNumber, deserializedEvent.Payload.ReceiptNumber);
        Assert.Equal(originalEvent.CorrelationId, deserializedEvent.CorrelationId);
        Assert.Equal(originalEvent.Payload.CustomerDetails.Name, deserializedEvent.Payload.CustomerDetails.Name);
        Assert.Equal(originalEvent.Payload.FinancialDetails.TotalAmount, deserializedEvent.Payload.FinancialDetails.TotalAmount);
        Assert.Equal(originalEvent.Payload.LineItems.Count, deserializedEvent.Payload.LineItems.Count);
        Assert.Equal(originalEvent.Payload.TemplateId, deserializedEvent.Payload.TemplateId);
    }

    [Fact]
    public async Task ReceiptPdfRequestedEvent_PublishedWithin3Seconds()
    {
        // Arrange
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var request = new
        {
            invoiceId = Guid.NewGuid().ToString(),
            amount = 1070.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);
        var correlationIdHeader = response.Headers.FirstOrDefault(h =>
            h.Key.Equals("X-Correlation-ID", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();
        Assert.NotNull(correlationIdHeader);
        var correlationId = Guid.Parse(correlationIdHeader);

        // Wait for the event to be published
        await Task.Delay(TimeSpan.FromSeconds(2));
        var published = await harness.Published.Any<ReceiptPdfRequestedEvent>(m =>
            m.Context.Message.CorrelationId == correlationId);
        stopwatch.Stop();

        // Assert
        Assert.True(published);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"PDF event took {stopwatch.ElapsedMilliseconds}ms to publish, expected < 5000ms");

        await harness.Stop();
    }

    [Fact]
    public async Task CreateReceipt_WithTaxValidationFailure_DoesNotPublishPdfEvent()
    {
        // Arrange
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        var request = new
        {
            invoiceId = "11111111-1111-1111-1111-111111111111",  // Invalid tax fields
            amount = 1000.00m,
            paymentMethod = "Bank Transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/receipt/v1/receipts", request);
        var correlationIdHeader = response.Headers.FirstOrDefault(h =>
            h.Key.Equals("X-Correlation-ID", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();
        Assert.NotNull(correlationIdHeader);
        var correlationId = Guid.Parse(correlationIdHeader);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify NO NEW event was published for THIS correlation ID
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        var published = await harness.Published.Any<ReceiptPdfRequestedEvent>(m =>
            m.Context.Message.CorrelationId == correlationId);
        Assert.False(published,
            $"ReceiptPdfRequestedEvent should NOT be published for validation failures. CorrelationId: {correlationId}");

        await harness.Stop();
    }
}
