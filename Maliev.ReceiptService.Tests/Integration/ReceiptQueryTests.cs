using System.Net;
using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

[Collection("IntegrationTests")]
public class ReceiptQueryTests : BaseReceiptIntegrationTest
{
    public ReceiptQueryTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task QueryReceipts_InvalidPage_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/receipt/v1/receipts?page=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task QueryReceipts_InvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/receipt/v1/receipts?pageSize=101");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task QueryReceipts_InvalidSortBy_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/receipt/v1/receipts?sortBy=invalid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task QueryReceipts_InvalidSortOrder_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/receipt/v1/receipts?sortOrder=invalid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
