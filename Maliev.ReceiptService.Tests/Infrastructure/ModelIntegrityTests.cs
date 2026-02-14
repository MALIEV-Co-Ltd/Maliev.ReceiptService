using Maliev.ReceiptService.Data.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.ReceiptService.Tests.Infrastructure;

public class ModelIntegrityTests
{
    [Fact]
    public void Model_ShouldNotHavePendingChanges()
    {
        var options = new DbContextOptionsBuilder<ReceiptDbContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        using var context = new ReceiptDbContext(options);
        var hasChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasChanges, "Run 'dotnet ef migrations add <Name> --project Maliev.ReceiptService.Data --startup-project Maliev.ReceiptService.Api'");
    }
}
