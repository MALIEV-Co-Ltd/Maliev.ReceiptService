using System.Net;
using System.Net.Http.Json;
using Maliev.Aspire.ServiceDefaults.Testing;
using Maliev.ReceiptService.Api.Services.IAM;
using Maliev.ReceiptService.Tests.Fixtures;

namespace Maliev.ReceiptService.Tests.Integration;

public class AuditAuthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuditAuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAuditHistory_WithAuditorPermission_ShouldSucceed()
    {
        // Arrange
        var token = _factory.CreateTestJwtToken("user-auditor", additionalClaims: new Dictionary<string, string>
        {
            ["permissions"] = ReceiptPermissions.Audit.Read
        });
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var receiptId = Guid.NewGuid();

        // Act
        // Expecting 404 Not Found if auth passes but receipt missing, or 200 OK.
        var response = await client.GetAsync($"/receipt/v1/receipts/{receiptId}/audit-history");

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditHistory_WithoutPermission_ShouldFail()
    {
        // Arrange
        var token = _factory.CreateTestJwtToken("user-viewer", additionalClaims: new Dictionary<string, string>
        {
            ["permissions"] = ReceiptPermissions.Receipts.Read
        });
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var receiptId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/receipt/v1/receipts/{receiptId}/audit-history");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
