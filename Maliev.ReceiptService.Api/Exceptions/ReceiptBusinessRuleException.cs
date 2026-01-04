namespace Maliev.ReceiptService.Api.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated for receipt operations
/// </summary>
public class ReceiptBusinessRuleException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptBusinessRuleException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ReceiptBusinessRuleException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptBusinessRuleException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ReceiptBusinessRuleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
