using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Pdf;
using Maliev.MessagingContracts.Contracts.Shared;
using Maliev.MessagingContracts.Contracts.Receipts;
using Maliev.ReceiptService.Infrastructure.Data;
using Maliev.ReceiptService.Domain.Entities;
using Maliev.ReceiptService.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.ReceiptService.Api.Consumers;

/// <summary>
/// Consumes PdfGenerationCompletedEvent from PDF Service to update receipt status
/// Handler Behavior:
/// 1. Find receipt by referenceId (receiptId)
/// 2. Update PdfReferenceId from PDF generation requestId
/// 3. Change Status from PendingPdf to Active
/// 4. Create audit event (EventType = PdfGenerated)
/// 5. Publish ReceiptGeneratedEvent
/// </summary>
public class PdfGeneratedEventConsumer : IConsumer<PdfGenerationCompletedEvent>
{
    private readonly ReceiptDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PdfGeneratedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfGeneratedEventConsumer"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="logger">The logger.</param>
    public PdfGeneratedEventConsumer(
        ReceiptDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<PdfGeneratedEventConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the <see cref="PdfGenerationCompletedEvent"/>.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Consume(ConsumeContext<PdfGenerationCompletedEvent> context)
    {
        var payload = context.Message.Payload;

        if (!string.Equals(payload.DocumentType, "Receipt", StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Skipping non-receipt document: DocumentType={DocumentType}, ReferenceId={ReferenceId}",
                payload.DocumentType,
                payload.ReferenceId);
            return;
        }

        // ReferenceId contains the ReceiptId
        if (!Guid.TryParse(payload.ReferenceId, out var receiptId))
        {
            _logger.LogError("Invalid ReferenceId format: {ReferenceId}", payload.ReferenceId);
            return;
        }

        _logger.LogInformation(
            "PDF generation completed event received for receipt {ReceiptId}, Request: {RequestId}, Storage: {StorageUrl}",
            receiptId, payload.RequestId, payload.StorageUrl);

        // Step 1: Find receipt by referenceId (receiptId)
        var receipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.Id == receiptId);

        if (receipt == null)
        {
            _logger.LogWarning(
                "Receipt {ReceiptId} not found for PDF generated event, correlation: {CorrelationId}",
                receiptId, context.Message.CorrelationId);
            return; // Idempotent - receipt might have been deleted
        }

        // Step 2 & 3: Update PdfReferenceId and change status
        var previousStatus = receipt.Status;

        // Store the PDF generation requestId as the reference
        if (Guid.TryParse(payload.RequestId, out var pdfRequestId))
        {
            receipt.PdfReferenceId = pdfRequestId;
        }

        receipt.Status = ReceiptStatus.Active;

        _logger.LogInformation(
            "Updating receipt {ReceiptNumber} status from {PreviousStatus} to {NewStatus}, PDF storage: {StorageUrl}",
            receipt.ReceiptNumber, previousStatus, receipt.Status, payload.StorageUrl);

        // Step 4: Create audit event
        var auditEvent = new ReceiptAuditEvent
        {
            Id = Guid.NewGuid(),
            ReceiptId = receipt.Id,
            EventType = AuditEventType.PdfGenerated,
            Timestamp = DateTime.UtcNow,
            StaffMemberId = "system", // PDF generation is system-initiated
            Reason = $"PDF generated successfully, storage: {payload.StorageUrl}",
            PreviousState = System.Text.Json.JsonSerializer.Serialize(new
            {
                receipt.Id,
                receipt.ReceiptNumber,
                Status = previousStatus,
                PdfReferenceId = (Guid?)null
            }),
            NewState = System.Text.Json.JsonSerializer.Serialize(new
            {
                receipt.Id,
                receipt.ReceiptNumber,
                receipt.Status,
                receipt.PdfReferenceId,
                StorageUrl = payload.StorageUrl
            }),
            CorrelationId = context.Message.CorrelationId,
            RetainUntil = DateTime.UtcNow.AddYears(7)
        };

        _context.ReceiptAuditEvents.Add(auditEvent);

        // Save changes
        await _context.SaveChangesAsync();

        // Step 5: Publish ReceiptGeneratedEvent (outbox guarantees atomic delivery with SaveChanges)
        await _publishEndpoint.Publish(new ReceiptGeneratedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "ReceiptGeneratedEvent",
            MessageType: Maliev.MessagingContracts.Contracts.Shared.MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "ReceiptService",
            ConsumedBy: ["NotificationService", "AnalyticsService"],
            CorrelationId: context.Message.CorrelationId,
            CausationId: context.Message.MessageId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new ReceiptGeneratedEventPayload(
                ReceiptId: receipt.Id.ToString(),
                ReceiptNumber: receipt.ReceiptNumber,
                StorageUrl: payload.StorageUrl,
                PdfReferenceId: receipt.PdfReferenceId?.ToString() ?? payload.RequestId,
                GeneratedAt: DateTimeOffset.UtcNow
            )
        ));

        _logger.LogInformation(
            "PDF callback processed and ReceiptGeneratedEvent published for receipt {ReceiptNumber}, status: {Status}, storage: {StorageUrl}",
            receipt.ReceiptNumber, receipt.Status, payload.StorageUrl);
    }
}
