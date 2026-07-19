namespace Maliev.ReceiptService.Application.Exceptions;

/// <summary>
/// Exception thrown when a receipt is not found
/// Part of Task T017 domain exceptions
/// </summary>
public class ReceiptNotFoundException : Exception
{
    /// <summary>
    /// Gets the receipt ID.
    /// </summary>
    public Guid ReceiptId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptNotFoundException"/> class.
    /// </summary>
    /// <param name="receiptId">The receipt ID.</param>
    /// <param name="message">The exception message.</param>
    public ReceiptNotFoundException(Guid receiptId, string message)
        : base(message)
    {
        ReceiptId = receiptId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptNotFoundException"/> class.
    /// </summary>
    /// <param name="receiptId">The receipt ID.</param>
    public ReceiptNotFoundException(Guid receiptId)
        : base($"Receipt {receiptId} not found")
    {
        ReceiptId = receiptId;
    }
}
