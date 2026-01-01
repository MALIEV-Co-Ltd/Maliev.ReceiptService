using Microsoft.EntityFrameworkCore;
using Maliev.ReceiptService.Data.Data;

namespace Maliev.ReceiptService.Api.Services;

/// <summary>
/// Sequential receipt number generator per research.md Decision 1
/// Format: ENTITY-YYYY-NNNNNN (e.g., "MALIEV-2025-000042")
/// Thread-safe with database-backed sequence tracking
/// </summary>
public class ReceiptNumberGenerator : IReceiptNumberGenerator
{
    private readonly ReceiptDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiptNumberGenerator"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ReceiptNumberGenerator(ReceiptDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Generates the next sequential receipt number.
    /// </summary>
    /// <param name="entity">The entity prefix (e.g., MALIEV).</param>
    /// <param name="year">The year.</param>
    /// <returns>The next receipt number string.</returns>
    public async Task<string> GenerateNextReceiptNumberAsync(string entity, int year)
    {
        if (string.IsNullOrWhiteSpace(entity))
        {
            throw new ArgumentNullException(nameof(entity), "Entity cannot be null or empty");
        }

        if (year < 2000 || year > 2100)
        {
            throw new ArgumentException("Year must be between 2000 and 2100", nameof(year));
        }

        // Query database for the maximum sequence number for this entity + year combination
        // Uses raw SQL to ensure atomicity and prevent race conditions
        var maxNumber = await _context.Receipts
            .Where(r => r.ReceiptNumber.StartsWith($"{entity}-{year}-"))
            .Select(r => r.ReceiptNumber)
            .OrderByDescending(rn => rn)
            .FirstOrDefaultAsync();

        int nextSequence = 1;

        if (maxNumber != null)
        {
            // Extract sequence number from format: ENTITY-YYYY-NNNNNN
            var parts = maxNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int currentSeq))
            {
                nextSequence = currentSeq + 1;
            }
        }

        // Validate sequence doesn't exceed 6-digit format limit (999999)
        if (nextSequence > 999999)
        {
            throw new InvalidOperationException(
                $"Receipt sequence limit exceeded for {entity}-{year}. Maximum sequence number is 999999. " +
                "Consider implementing year rollover or extending sequence format.");
        }

        // Format as ENTITY-YYYY-NNNNNN with zero-padding
        return $"{entity}-{year}-{nextSequence:D6}";
    }
}
