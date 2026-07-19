namespace Maliev.ReceiptService.Application.Exceptions;

/// <summary>
/// Exception thrown when a duplicate receipt is attempted to be created.
/// </summary>
public class DuplicateReceiptException : Exception
{
    /// <summary>
    /// Gets the invoice ID.
    /// </summary>
    public Guid InvoiceId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateReceiptException"/> class.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="message">The exception message.</param>
    public DuplicateReceiptException(Guid invoiceId, string message) : base(message)
    {
        InvoiceId = invoiceId;
    }
}
