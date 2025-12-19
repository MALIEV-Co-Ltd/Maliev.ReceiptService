namespace Maliev.ReceiptService.Api.Exceptions;

public class OverReceiptingException : Exception
{
    public Guid InvoiceId { get; }
    public decimal RequestedAmount { get; }
    public decimal RemainingBalance { get; }

    public OverReceiptingException(Guid invoiceId, decimal requestedAmount, decimal remainingBalance, string message) : base(message)
    {
        InvoiceId = invoiceId;
        RequestedAmount = requestedAmount;
        RemainingBalance = remainingBalance;
    }
}
