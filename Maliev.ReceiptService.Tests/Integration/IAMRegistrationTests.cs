using Maliev.Aspire.ServiceDefaults.Testing;
using Maliev.ReceiptService.Api.Services.IAM;
using Maliev.ReceiptService.Application.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Maliev.ReceiptService.Tests.Integration;

[Collection("IntegrationTests")]
public class IAMRegistrationTests
{
    private readonly TestWebApplicationFactory _factory;

    public IAMRegistrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ReceiptPermissions_ShouldBeDefined()
    {
        Assert.NotEmpty(ReceiptPermissions.All);
        Assert.Contains(ReceiptPermissions.Receipts.Create, ReceiptPermissions.All);
        Assert.Contains(ReceiptPermissions.Receipts.Void, ReceiptPermissions.All);
    }

    [Fact]
    public void PredefinedRoles_ShouldBeDefined()
    {
        var roles = ReceiptPredefinedRoles.All;
        Assert.NotEmpty(roles);
        Assert.Contains(roles, r => r.RoleId == ReceiptPredefinedRoles.Admin);
        Assert.Contains(roles, r => r.RoleId == ReceiptPredefinedRoles.Manager);
        Assert.Contains(roles, r => r.RoleId == ReceiptPredefinedRoles.Creator);
    }

    [Fact]
    public void IAMRegistrationService_ShouldBeRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider.GetServices<IHostedService>();

        // BackgroundIAMRegistrationService wraps the actual ReceiptIAMRegistrationService
        Assert.Contains(services, s => s.GetType().Name.Contains("BackgroundIAMRegistrationService") ||
                                        s is ReceiptIAMRegistrationService);
    }
}
