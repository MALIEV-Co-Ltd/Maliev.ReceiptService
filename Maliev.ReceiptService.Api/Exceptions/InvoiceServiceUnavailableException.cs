namespace Maliev.ReceiptService.Api.Exceptions;

/// <summary>
/// Exception thrown when the Invoice Service is unavailable or returns a transient error.
/// </summary>
public class InvoiceServiceUnavailableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceServiceUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public InvoiceServiceUnavailableException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceServiceUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvoiceServiceUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
