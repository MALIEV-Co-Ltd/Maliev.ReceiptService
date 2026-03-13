using Maliev.ReceiptService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Maliev.ReceiptService.Api.Services;

/// <summary>
/// Sequential receipt number generator per research.md Decision 1
/// Format: ENTITY-YYYY-NNNNNN (e.g., "MALIEV-2025-000042")
/// Thread-safe with PostgreSQL sequence for atomic generation
/// </summary>
/// <remarks>
/// TODO: [ARCH-DEBT] This generator should be moved to the Application layer
/// per Clean Architecture (Api → Application → Domain ← Infrastructure).
/// </remarks>
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

        // Use PostgreSQL sequence for atomic, thread-safe number generation
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        int nextVal;
        using (var command = new NpgsqlCommand("SELECT nextval('receipt_number_seq')", (NpgsqlConnection)connection))
        {
            var result = await command.ExecuteScalarAsync();
            nextVal = Convert.ToInt32(result);
        }

        // Validate sequence doesn't exceed 6-digit format limit (999999)
        if (nextVal > 999999)
        {
            throw new InvalidOperationException(
                $"Receipt sequence limit exceeded for {entity}-{year}. Maximum sequence number is 999999. " +
                "Consider implementing year rollover or extending sequence format.");
        }

        // Format as ENTITY-YYYY-NNNNNN with zero-padding
        return $"{entity}-{year}-{nextVal:D6}";
    }
}
