using Maliev.ReceiptService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.ReceiptService.Tests.Infrastructure;

public class ModelIntegrityTests
{
    [Fact(Skip = "Model integrity check requires migrations - using EnsureCreated instead")]
    public void Model_ShouldNotHavePendingChanges()
    {
        var options = new DbContextOptionsBuilder<ReceiptDbContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        using var context = new ReceiptDbContext(options);
        var hasChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasChanges, "Run 'dotnet ef migrations add <Name> --project Maliev.ReceiptService.Infrastructure --startup-project Maliev.ReceiptService.Api'");
    }
}
