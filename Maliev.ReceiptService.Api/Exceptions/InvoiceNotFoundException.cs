namespace Maliev.ReceiptService.Api.Exceptions;

public class InvoiceNotFoundException : Exception
{
    public Guid InvoiceId { get; }

    public InvoiceNotFoundException(Guid invoiceId, string message) : base(message)
    {
        InvoiceId = invoiceId;
    }
}
