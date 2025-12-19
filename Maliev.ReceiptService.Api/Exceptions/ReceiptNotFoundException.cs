namespace Maliev.ReceiptService.Api.Exceptions;

/// <summary>
/// Exception thrown when a receipt is not found
/// Part of Task T017 domain exceptions
/// </summary>
public class ReceiptNotFoundException : Exception
{
    public Guid ReceiptId { get; }

    public ReceiptNotFoundException(Guid receiptId, string message)
        : base(message)
    {
        ReceiptId = receiptId;
    }

    public ReceiptNotFoundException(Guid receiptId)
        : base($"Receipt {receiptId} not found")
    {
        ReceiptId = receiptId;
    }
}
