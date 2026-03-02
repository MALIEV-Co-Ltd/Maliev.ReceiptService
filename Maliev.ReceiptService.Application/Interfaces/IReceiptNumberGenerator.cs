namespace Maliev.ReceiptService.Application.Interfaces;

public interface IReceiptNumberGenerator
{
    Task<string> GenerateNextReceiptNumberAsync(string prefix, int year);
}
