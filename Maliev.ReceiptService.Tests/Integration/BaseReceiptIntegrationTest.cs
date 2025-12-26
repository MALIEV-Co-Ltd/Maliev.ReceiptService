using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

/// <summary>
/// Base class for ReceiptService integration tests.
/// Provides authenticated HTTP client with all receipt permissions by default.
/// </summary>
[Collection("IntegrationTests")]
public abstract class BaseReceiptIntegrationTest : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly TestWebApplicationFactory Factory;

    protected BaseReceiptIntegrationTest(TestWebApplicationFactory factory)
    {
        Factory = factory;
        // Use authenticated client with all permissions by default
        Client = factory.CreateAuthenticatedClientWithAllPermissions();
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        await Factory.CleanDatabaseAsync();
        Factory.ClearCache();
    }
}
