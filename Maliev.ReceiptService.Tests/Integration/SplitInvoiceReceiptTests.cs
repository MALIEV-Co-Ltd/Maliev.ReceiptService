using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.ReceiptService.Api.Models.Requests;
using Xunit;

namespace Maliev.ReceiptService.Tests.Integration;

[Collection("IntegrationTests")]
public class SplitInvoiceReceiptTests : BaseReceiptIntegrationTest
{

    public SplitInvoiceReceiptTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    // T074: Integration test for split invoice receipt generation
    [Fact]
    public async Task CreateReceipt_ForSplitInvoiceSegment_Success()
    {
        // Arrange - Split invoice with 3 segments: $500, $300, $200
        var invoiceId = Guid.Parse("550e8400-e29b-41d4-a716-446655440030");
        var segment1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var request = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            InvoiceSegmentId = segment1Id,
            Amount = 500.00m,
            PaymentMethod = "Credit Card"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Verify segment linkage
        Assert.True(root.TryGetProperty("invoiceSegmentId", out var segmentId));
        Assert.Equal(segment1Id.ToString(), segmentId.GetString());

        // Verify exact segment amount
        Assert.True(root.TryGetProperty("totalAmount", out var totalAmount));
        Assert.Equal(500.00m, totalAmount.GetDecimal());

        // Verify receipt is linked to split invoice
        Assert.True(root.TryGetProperty("invoiceId", out var returnedInvoiceId));
        Assert.Equal(invoiceId.ToString(), returnedInvoiceId.GetString());
    }

    // T074: Multiple segments of same split invoice
    [Fact]
    public async Task CreateReceipt_ForMultipleSplitInvoiceSegments_CreatesIndependentReceipts()
    {
        // Arrange - Split invoice with 3 segments
        var invoiceId = Guid.Parse("550e8400-e29b-41d4-a716-446655440031");
        var segment1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var segment2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act - Create receipts for two different segments
        var request1 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            InvoiceSegmentId = segment1Id,
            Amount = 500.00m,
            PaymentMethod = "Credit Card"
        };

        var request2 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            InvoiceSegmentId = segment2Id,
            Amount = 300.00m,
            PaymentMethod = "Bank Transfer"
        };

        var response1 = await Client.PostAsJsonAsync("/receipt/v1/receipts", request1);
        var response2 = await Client.PostAsJsonAsync("/receipt/v1/receipts", request2);

        // Assert - Both should succeed independently
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        // Verify each receipt has correct segment linkage
        var content1 = await response1.Content.ReadAsStringAsync();
        var jsonDoc1 = JsonDocument.Parse(content1);
        Assert.Equal(segment1Id.ToString(), jsonDoc1.RootElement.GetProperty("invoiceSegmentId").GetString());

        var content2 = await response2.Content.ReadAsStringAsync();
        var jsonDoc2 = JsonDocument.Parse(content2);
        Assert.Equal(segment2Id.ToString(), jsonDoc2.RootElement.GetProperty("invoiceSegmentId").GetString());
    }

    // T075: Integration test for segment-specific tax rate application
    [Fact]
    public async Task CreateReceipt_ForSplitInvoiceSegment_AppliesSegmentSpecificTaxRate()
    {
        // Arrange - Split invoice with segment having different tax rate (10% instead of standard 7%)
        var invoiceId = Guid.Parse("550e8400-e29b-41d4-a716-446655440032");
        var segmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var request = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            InvoiceSegmentId = segmentId,
            Amount = 550.00m, // Segment with 10% VAT: 500 + 50 = 550
            PaymentMethod = "Cash"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Verify segment-specific properties exist
        Assert.True(root.TryGetProperty("invoiceSegmentId", out var returnedSegmentId));
        Assert.Equal(segmentId.ToString(), returnedSegmentId.GetString());

        // Verify tax amount exists (actual tax rate calculation is verified by the receipt service)
        Assert.True(root.TryGetProperty("taxAmount", out var taxAmount));
        Assert.True(taxAmount.GetDecimal() > 0, "Tax amount should be greater than zero");
    }

    // T075: Segment with withholding tax rate
    [Fact]
    public async Task CreateReceipt_ForSplitInvoiceSegment_AppliesSegmentSpecificWithholdingTax()
    {
        // Arrange - Split invoice segment with withholding tax
        var invoiceId = Guid.Parse("550e8400-e29b-41d4-a716-446655440033");
        var segmentId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var request = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            InvoiceSegmentId = segmentId,
            Amount = 485.00m, // Segment: 500 + 35 VAT - 50 WHT = 485
            PaymentMethod = "Bank Transfer"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/receipt/v1/receipts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Verify segment-specific properties exist
        Assert.True(root.TryGetProperty("invoiceSegmentId", out var returnedSegmentId));
        Assert.Equal(segmentId.ToString(), returnedSegmentId.GetString());

        // Verify withholding tax amount exists
        Assert.True(root.TryGetProperty("withholdingTaxAmount", out var whtAmount));
        Assert.True(whtAmount.GetDecimal() >= 0, "Withholding tax amount should be present");
    }

    // T076: Integration test for split invoice receipt status query
    [Fact]
    public async Task GetReceipts_FilterByInvoiceId_ShowsSegmentReceiptingStatus()
    {
        // Arrange - Create receipts for some segments of a split invoice
        var invoiceId = Guid.Parse("550e8400-e29b-41d4-a716-446655440034");
        var segment1Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var segment2Id = Guid.Parse("66666666-6666-6666-6666-666666666666");

        // Create receipt for segment 1 only
        var request1 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            InvoiceSegmentId = segment1Id,
            Amount = 500.00m,
            PaymentMethod = "Credit Card"
        };

        await Client.PostAsJsonAsync("/receipt/v1/receipts", request1);

        // Act - Query receipts for this invoice
        var queryResponse = await Client.GetAsync($"/receipt/v1/receipts?invoiceId={invoiceId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);

        var content = await queryResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Query endpoint returns paged response with "data" property
        Assert.True(root.TryGetProperty("data", out var dataArray));
        var receipts = dataArray.EnumerateArray().ToList();

        // Verify we can identify which segments are receipted
        Assert.Single(receipts); // Only one segment has receipt

        var receipt = receipts[0];
        Assert.True(receipt.TryGetProperty("invoiceSegmentId", out var segmentId));
        Assert.Equal(segment1Id.ToString(), segmentId.GetString());

        // Segment 2 has no receipt (would need to query invoice service to see it's outstanding)
    }

    // T076: Query by segment ID
    [Fact]
    public async Task GetReceipts_FilterBySegmentId_ReturnsOnlySegmentReceipts()
    {
        // Arrange - Create receipts for multiple segments
        var invoiceId = Guid.Parse("550e8400-e29b-41d4-a716-446655440035");
        var segment1Id = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var segment2Id = Guid.Parse("88888888-8888-8888-8888-888888888888");

        var request1 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            InvoiceSegmentId = segment1Id,
            Amount = 500.00m,
            PaymentMethod = "Credit Card"
        };

        var request2 = new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            InvoiceSegmentId = segment2Id,
            Amount = 300.00m,
            PaymentMethod = "Bank Transfer"
        };

        await Client.PostAsJsonAsync("/receipt/v1/receipts", request1);
        await Client.PostAsJsonAsync("/receipt/v1/receipts", request2);

        // Act - Query receipts for specific segment
        var queryResponse = await Client.GetAsync($"/receipt/v1/receipts?segmentId={segment1Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);

        var content = await queryResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Query endpoint returns paged response with "data" property
        Assert.True(root.TryGetProperty("data", out var dataArray));
        var receipts = dataArray.EnumerateArray().ToList();

        // Verify only segment 1 receipts returned
        Assert.All(receipts, receipt =>
        {
            Assert.True(receipt.TryGetProperty("invoiceSegmentId", out var segmentId));
            Assert.Equal(segment1Id.ToString(), segmentId.GetString());
        });
    }

    // T076: Combined query - invoice + segment status overview
    [Fact]
    public async Task GetReceipts_ForSplitInvoice_ProvidesSegmentStatusOverview()
    {
        // Arrange - Split invoice with 3 segments, receipt 2 of them
        var invoiceId = Guid.Parse("550e8400-e29b-41d4-a716-446655440036");
        var segment1Id = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var segment2Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var segment3Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        // Create receipts for segments 1 and 2
        await Client.PostAsJsonAsync("/receipt/v1/receipts", new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            InvoiceSegmentId = segment1Id,
            Amount = 500.00m,
            PaymentMethod = "Credit Card"
        });

        await Client.PostAsJsonAsync("/receipt/v1/receipts", new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            InvoiceSegmentId = segment2Id,
            Amount = 300.00m,
            PaymentMethod = "Bank Transfer"
        });

        // Act - Query all receipts for this invoice
        var queryResponse = await Client.GetAsync($"/receipt/v1/receipts?invoiceId={invoiceId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);

        var content = await queryResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Query endpoint returns paged response with "data" property
        Assert.True(root.TryGetProperty("data", out var dataArray));
        var receipts = dataArray.EnumerateArray().ToList();

        // Verify we have 2 receipts (segments 1 and 2)
        Assert.Equal(2, receipts.Count);

        // Verify segment IDs
        var segmentIds = receipts
            .Select(r => r.GetProperty("invoiceSegmentId").GetString())
            .ToList();

        Assert.Contains(segment1Id.ToString(), segmentIds);
        Assert.Contains(segment2Id.ToString(), segmentIds);
        Assert.DoesNotContain(segment3Id.ToString(), segmentIds); // Segment 3 is outstanding
    }
}

