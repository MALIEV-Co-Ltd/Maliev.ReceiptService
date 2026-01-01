namespace Maliev.ReceiptService.Api.Exceptions;

/// <summary>
/// Exception thrown when an invoice is not found.
/// </summary>
public class InvoiceNotFoundException : Exception
{
    /// <summary>
    /// Gets the invoice ID.
    /// </summary>
    public Guid InvoiceId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceNotFoundException"/> class.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="message">The exception message.</param>
    public InvoiceNotFoundException(Guid invoiceId, string message) : base(message)
    {
        InvoiceId = invoiceId;
    }
}
