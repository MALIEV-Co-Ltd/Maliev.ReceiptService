using Testcontainers.RabbitMq;

namespace Maliev.ReceiptService.Tests.Fixtures;

public class TestRabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container;

    public TestRabbitMqFixture()
    {
        _container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management-alpine")
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
