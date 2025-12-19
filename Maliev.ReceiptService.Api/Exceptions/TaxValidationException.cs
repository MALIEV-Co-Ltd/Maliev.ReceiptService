namespace Maliev.ReceiptService.Api.Exceptions;

public class TaxValidationException : Exception
{
    public List<string> ValidationErrors { get; }

    public TaxValidationException(List<string> validationErrors, string message) : base(message)
    {
        ValidationErrors = validationErrors;
    }
}
