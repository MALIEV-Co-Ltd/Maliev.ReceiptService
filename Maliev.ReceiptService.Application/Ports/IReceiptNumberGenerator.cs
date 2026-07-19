namespace Maliev.ReceiptService.Application.Ports;

/// <summary>
/// Receipt number generator interface per research.md Decision 1
/// Generates sequential receipt numbers in format: ENTITY-YYYY-NNNNNN
/// </summary>
public interface IReceiptNumberGenerator
{
    /// <summary>
    /// Generates the next sequential receipt number for the given entity and year
    /// </summary>
    /// <param name="entity">Legal entity prefix (e.g., "MALIEV")</param>
    /// <param name="year">Year for the receipt number</param>
    /// <returns>Formatted receipt number (e.g., "MALIEV-2025-000042")</returns>
    Task<string> GenerateNextReceiptNumberAsync(string entity, int year);
}
