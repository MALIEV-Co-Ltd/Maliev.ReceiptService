using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.ReceiptService.Data.Data;

/// <summary>
/// Design-time factory for creating ReceiptDbContext during migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ReceiptDbContext>
{
    public ReceiptDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReceiptDbContext>();

        // Use a dummy connection string for migrations - the actual connection string
        // will be configured at runtime via ServiceDefaults
        optionsBuilder.UseNpgsql("Host=localhost;Database=receipts;Username=postgres;Password=postgres");

        return new ReceiptDbContext(optionsBuilder.Options);
    }
}
