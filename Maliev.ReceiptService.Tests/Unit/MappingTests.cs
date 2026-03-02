using Maliev.ReceiptService.Api.Extensions;
using Maliev.ReceiptService.Domain.Entities;
using Maliev.ReceiptService.Domain.Enums;
using Xunit;

namespace Maliev.ReceiptService.Tests.Unit;

public class MappingTests
{
    [Fact]
    public void ToResponse_MapsCorrectly()
    {
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = "R-001",
            Status = ReceiptStatus.Active,
            LineItems = new List<ReceiptLineItem>
            {
                new() { LineNumber = 1, Description = "Desc", Quantity = 1, UnitPrice = 100, TaxRate = 7, LineTotal = 107 }
            }
        };

        var response = receipt.ToResponse();

        Assert.Equal(receipt.Id, response.Id);
        Assert.Single(response.LineItems);
        Assert.Equal("Desc", response.LineItems[0].Description);
    }
}
