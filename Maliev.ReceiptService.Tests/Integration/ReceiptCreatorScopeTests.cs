using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.ReceiptService.Application.Authorization;
using Maliev.ReceiptService.Application.Models.Requests;
using Maliev.ReceiptService.Application.Models.Responses;
using WireMock.RequestBuilders;
using WireMockResponse = WireMock.ResponseBuilders.Response;

namespace Maliev.ReceiptService.Tests.Integration;

public class ReceiptCreatorScopeTests : BaseReceiptIntegrationTest
{
    public ReceiptCreatorScopeTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreatorRole_CannotCreateReceiptForInvoiceCreatedByAnotherPrincipal()
    {
        await Factory.CleanDatabaseAsync();
        var invoiceId = Guid.NewGuid();
        StubInvoice(invoiceId, "creator-a");
        using var creatorB = CreateCreatorClient("creator-b");

        var response = await creatorB.PostAsJsonAsync("/receipt/v1/receipts", CreateReceiptRequest(invoiceId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatorRole_CanOnlyReadReceiptsTheyCreated()
    {
        await Factory.CleanDatabaseAsync();
        var creatorAInvoiceId = Guid.NewGuid();
        var creatorBInvoiceId = Guid.NewGuid();
        StubInvoice(creatorAInvoiceId, "creator-a");
        StubInvoice(creatorBInvoiceId, "creator-b");
        using var creatorA = CreateCreatorClient("creator-a");
        using var creatorB = CreateCreatorClient("creator-b");

        var creatorAReceiptId = await CreateReceiptAsync(creatorA, creatorAInvoiceId);
        var creatorBReceiptId = await CreateReceiptAsync(creatorB, creatorBInvoiceId);

        var forbiddenResponse = await creatorB.GetAsync($"/receipt/v1/receipts/{creatorAReceiptId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        var allowedResponse = await creatorB.GetAsync($"/receipt/v1/receipts/{creatorBReceiptId}");
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
    }

    [Fact]
    public async Task CreatorRole_QueryOnlyReturnsReceiptsTheyCreated()
    {
        await Factory.CleanDatabaseAsync();
        var creatorAInvoiceId = Guid.NewGuid();
        var creatorBInvoiceId = Guid.NewGuid();
        StubInvoice(creatorAInvoiceId, "creator-a");
        StubInvoice(creatorBInvoiceId, "creator-b");
        using var creatorA = CreateCreatorClient("creator-a");
        using var creatorB = CreateCreatorClient("creator-b", ReceiptPermissions.Receipts.Query);

        await CreateReceiptAsync(creatorA, creatorAInvoiceId);
        var creatorBReceiptId = await CreateReceiptAsync(creatorB, creatorBInvoiceId);

        var response = await creatorB.GetAsync("/receipt/v1/receipts?pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ReceiptResponse>>();
        Assert.NotNull(result);
        var receipt = Assert.Single(result!.Data);
        Assert.Equal(creatorBReceiptId, receipt.Id);
        Assert.Equal(1, result.Pagination.TotalCount);
    }

    private HttpClient CreateCreatorClient(string userId, params string[] extraPermissions)
    {
        var permissions = ReceiptPredefinedRoles.All
            .Single(role => role.RoleId == ReceiptPredefinedRoles.Creator)
            .Permissions
            .Concat(extraPermissions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Factory.CreateAuthenticatedClient(
            userId,
            [ReceiptPredefinedRoles.Creator],
            permissions);
    }

    private async Task<Guid> CreateReceiptAsync(HttpClient client, Guid invoiceId)
    {
        var response = await client.PostAsJsonAsync("/receipt/v1/receipts", CreateReceiptRequest(invoiceId));
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Receipt creation failed: {response.StatusCode} - {error}");
        }

        var receipt = await response.Content.ReadFromJsonAsync<ReceiptResponse>();
        Assert.NotNull(receipt);
        return receipt!.Id;
    }

    private static CreateReceiptRequest CreateReceiptRequest(Guid invoiceId)
    {
        return new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 1070.00m,
            PaymentMethod = "Bank Transfer"
        };
    }

    private void StubInvoice(Guid invoiceId, string createdBy)
    {
        Factory.InvoiceServiceMock
            .Given(Request.Create().WithPath($"/invoice/v1/invoices/{invoiceId}").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = invoiceId,
                    invoiceNumber = $"INV-{invoiceId:N}"[..12],
                    customerId = Guid.NewGuid(),
                    customerName = "Scoped Test Customer",
                    customerTaxId = "1234567890123",
                    customerAddress = "123 Test Street, Bangkok 10100",
                    sellerTaxId = "9876543210987",
                    currency = "THB",
                    subtotal = 1000.00m,
                    vatRate = 7.0m,
                    taxAmount = 70.00m,
                    withholdingTaxRate = 0.0m,
                    withholdingTaxAmount = 0.0m,
                    withholdingTaxType = "None",
                    totalAmount = 1070.00m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    totalPaidAmount = 0m,
                    remainingBalance = 1070.00m,
                    paymentStatus = "Unpaid",
                    createdAt = DateTime.UtcNow,
                    createdBy,
                    lineItems = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 1,
                            description = "Scoped Line Item",
                            quantity = 1m,
                            unitPrice = 1000.00m,
                            taxRate = 7.0m,
                            lineTotal = 1070.00m
                        }
                    }
                })));
    }
}
