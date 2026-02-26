namespace Maliev.ReceiptService.Api.Exceptions;

/// <summary>
/// Exception thrown when a receipt amount exceeds the remaining balance of an invoice.
/// </summary>
public class OverReceiptingConflictException : Exception
{
    /// <summary>
    /// Gets the invoice ID.
    /// </summary>
    public Guid InvoiceId { get; }

    /// <summary>
    /// Gets the requested receipt amount.
    /// </summary>
    public decimal RequestedAmount { get; }

    /// <summary>
    /// Gets the remaining balance of the invoice.
    /// </summary>
    public decimal RemainingBalance { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OverReceiptingConflictException"/> class.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="requestedAmount">The requested amount.</param>
    /// <param name="remainingBalance">The remaining balance.</param>
    /// <param name="message">The exception message.</param>
    public OverReceiptingConflictException(Guid invoiceId, decimal requestedAmount, decimal remainingBalance, string message) : base(message)
    {
        InvoiceId = invoiceId;
        RequestedAmount = requestedAmount;
        RemainingBalance = remainingBalance;
    }
}
