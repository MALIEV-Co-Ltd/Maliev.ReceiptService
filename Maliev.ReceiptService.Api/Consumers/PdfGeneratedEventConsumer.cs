using MassTransit;
using Microsoft.EntityFrameworkCore;
using Maliev.ReceiptService.Data.Data;
using Maliev.ReceiptService.Api.Events;
using Maliev.ReceiptService.Data.Models.Entities;
using Maliev.ReceiptService.Data.Models.Enums;

namespace Maliev.ReceiptService.Api.Consumers;

/// <summary>
/// Consumes PdfGeneratedEvent from PDF Service to update receipt status
/// Task: T106 [P] Create PdfGeneratedEventConsumer
/// Per contracts/message-contracts.md - Consumed Events
/// Handler Behavior:
/// 1. Find receipt by receiptId
/// 2. Update PdfReferenceId to pdfReferenceId
/// 3. Change Status from PendingPdf to Active
/// 4. Create audit event (EventType = PdfGenerated)
/// </summary>
public class PdfGeneratedEventConsumer : IConsumer<PdfGeneratedEvent>
{
    private readonly ReceiptDbContext _context;
    private readonly ILogger<PdfGeneratedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfGeneratedEventConsumer"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public PdfGeneratedEventConsumer(
        ReceiptDbContext context,
        ILogger<PdfGeneratedEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the <see cref="PdfGeneratedEvent"/>.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Consume(ConsumeContext<PdfGeneratedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "PDF generated event received for receipt {ReceiptId}, PDF reference: {PdfReferenceId}, correlation: {CorrelationId}",
            message.ReceiptId, message.PdfReferenceId, message.CorrelationId);

        // Step 1: Find receipt by receiptId
        var receipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.Id == message.ReceiptId);

        if (receipt == null)
        {
            _logger.LogWarning(
                "Receipt {ReceiptId} not found for PDF generated event, correlation: {CorrelationId}",
                message.ReceiptId, message.CorrelationId);
            return; // Idempotent - receipt might have been deleted
        }

        // Step 2 & 3: Update PdfReferenceId and change status
        var previousStatus = receipt.Status;
        receipt.PdfReferenceId = message.PdfReferenceId;
        receipt.Status = ReceiptStatus.Active;

        _logger.LogInformation(
            "Updating receipt {ReceiptNumber} status from {PreviousStatus} to {NewStatus}, PDF reference: {PdfReferenceId}",
            receipt.ReceiptNumber, previousStatus, receipt.Status, message.PdfReferenceId);

        // Step 4: Create audit event
        var auditEvent = new ReceiptAuditEvent
        {
            Id = Guid.NewGuid(),
            ReceiptId = receipt.Id,
            EventType = AuditEventType.PdfGenerated,
            Timestamp = DateTime.UtcNow,
            StaffMemberId = "system", // PDF generation is system-initiated
            Reason = $"PDF generated successfully, reference: {message.PdfReferenceId}",
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
                receipt.PdfReferenceId
            }),
            CorrelationId = message.CorrelationId,
            RetainUntil = DateTime.UtcNow.AddYears(7)
        };

        _context.ReceiptAuditEvents.Add(auditEvent);

        // Save changes
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "PDF callback processed successfully for receipt {ReceiptNumber}, status: {Status}, PDF: {PdfUrl}",
            receipt.ReceiptNumber, receipt.Status, message.UploadServiceUrl);
    }
}
