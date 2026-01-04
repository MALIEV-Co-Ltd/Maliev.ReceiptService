using System.Net;
using System.Net.Http.Json;
using Maliev.Aspire.ServiceDefaults.Testing;
using Maliev.ReceiptService.Api.Models.Requests;
using Maliev.ReceiptService.Api.Services.IAM;
using Maliev.ReceiptService.Tests.Fixtures;

namespace Maliev.ReceiptService.Tests.Integration;

public class ReceiptVoidAuthTests
{
    private readonly TestWebApplicationFactory _factory;

    public ReceiptVoidAuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task VoidReceipt_WithVoidPermission_ShouldSucceed()
    {
        // Arrange
        var token = _factory.CreateTestJwtToken("user-admin", additionalClaims: new Dictionary<string, string>
        {
            ["permissions"] = ReceiptPermissions.Receipts.Void
        });
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new VoidReceiptRequest { Reason = "Mistake" };
        var receiptId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/void", request);

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VoidReceipt_WithoutPermission_ShouldFail()
    {
        // Arrange
        var token = _factory.CreateTestJwtToken("user-creator", additionalClaims: new Dictionary<string, string>
        {
            ["permissions"] = ReceiptPermissions.Receipts.Create
        });
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new VoidReceiptRequest { Reason = "Mistake" };
        var receiptId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync($"/receipt/v1/receipts/{receiptId}/void", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
