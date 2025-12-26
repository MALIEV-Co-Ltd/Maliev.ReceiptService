using System.Net;
using System.Net.Http.Json;
using Maliev.Aspire.ServiceDefaults.Testing;
using Maliev.ReceiptService.Api.Models.Requests;
using Maliev.ReceiptService.Api.Services.IAM;
using Maliev.ReceiptService.Tests.Fixtures;

namespace Maliev.ReceiptService.Tests.Integration;

public class ReceiptCreationAuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ReceiptCreationAuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateReceipt_WithCreatorPermission_ShouldSucceed()
    {
        // Arrange
        var token = _factory.CreateTestJwtToken("user-creator", additionalClaims: new Dictionary<string, string>
        {
            ["permissions"] = ReceiptPermissions.Receipts.Create
        });
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 100,
            PaymentMethod = "CreditCard"
        };

        // Act
        // Note: Actual logic might fail if InvoiceId doesn't exist, but we check Authorization logic first (401/403 vs 404/400)
        // Since standard behavior is Authorize filter runs before action, getting 404/400/201 means Auth passed.
        // Getting 401/403 means Auth failed.
        var response = await client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateReceipt_WithoutPermission_ShouldFail()
    {
        // Arrange
        var token = _factory.CreateTestJwtToken("user-viewer", additionalClaims: new Dictionary<string, string>
        {
            ["permissions"] = ReceiptPermissions.Receipts.Read
        });
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 100,
            PaymentMethod = "CreditCard"
        };

        // Act
        var response = await client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
