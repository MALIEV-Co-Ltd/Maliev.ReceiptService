namespace Maliev.ReceiptService.Api.Exceptions;

public class DuplicateReceiptException : Exception
{
    public Guid InvoiceId { get; }

    public DuplicateReceiptException(Guid invoiceId, string message) : base(message)
    {
        InvoiceId = invoiceId;
    }
}
