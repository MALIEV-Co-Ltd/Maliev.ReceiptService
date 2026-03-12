using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.ReceiptService.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating ReceiptDbContext for EF Core migrations
/// </summary>
public class ReceiptDbContextFactory : IDesignTimeDbContextFactory<ReceiptDbContext>
{
    /// <summary>
    /// Creates a new instance of <see cref="ReceiptDbContext"/> for design-time use.
    /// </summary>
    /// <param name="args">Arguments passed by the design-time tool.</param>
    /// <returns>A new instance of the database context.</returns>
    public ReceiptDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReceiptDbContext>();

        // Use a dummy connection string for design-time operations
        // The actual connection string will be configured in Program.cs
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=receipt;Username=postgres;Password=postgres"
        );

        return new ReceiptDbContext(optionsBuilder.Options);
    }
}
