namespace Maliev.ReceiptService.Api.Exceptions;

/// <summary>
/// Exception thrown when tax validation fails.
/// </summary>
public class TaxValidationException : Exception
{
    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public List<string> ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaxValidationException"/> class.
    /// </summary>
    /// <param name="validationErrors">The validation errors.</param>
    /// <param name="message">The exception message.</param>
    public TaxValidationException(List<string> validationErrors, string message) : base(message)
    {
        ValidationErrors = validationErrors;
    }
}
