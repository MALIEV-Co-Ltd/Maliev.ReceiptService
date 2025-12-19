namespace Maliev.ReceiptService.Api.Events;

/// <summary>
/// Event consumed from PDF Service when PDF generation is complete
/// Task: T105 [P] Create PdfGeneratedEvent consumer DTO
/// Per contracts/message-contracts.md - Consumed Events
/// Routing Key: maliev.pdf.v1.pdf.generated
/// </summary>
public class PdfGeneratedEvent
{
    /// <summary>
    /// Original receipt ID for correlation
    /// </summary>
    public required Guid ReceiptId { get; set; }

    /// <summary>
    /// PDF file reference in Upload Service
    /// </summary>
    public required Guid PdfReferenceId { get; set; }

    /// <summary>
    /// Direct URL to download PDF
    /// </summary>
    public required string UploadServiceUrl { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public required Guid CorrelationId { get; set; }

    /// <summary>
    /// Event timestamp
    /// </summary>
    public required DateTime Timestamp { get; set; }
}
