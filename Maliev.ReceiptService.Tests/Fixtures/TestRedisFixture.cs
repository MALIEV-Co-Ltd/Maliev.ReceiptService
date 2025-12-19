using Testcontainers.Redis;

namespace Maliev.ReceiptService.Tests.Fixtures;

public class TestRedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container;

    public TestRedisFixture()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
