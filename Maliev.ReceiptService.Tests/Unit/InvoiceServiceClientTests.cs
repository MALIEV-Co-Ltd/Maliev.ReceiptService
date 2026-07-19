using System.Net;
using System.Net.Http.Json;
using Maliev.ReceiptService.Infrastructure.ExternalServices;
using Maliev.ReceiptService.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maliev.ReceiptService.Tests.Unit;

public class InvoiceServiceClientTests
{
    [Fact]
    public async Task GetInvoiceAsync_WithInvoiceServiceWireShape_MapsReceiptInvoiceDto()
    {
        var invoiceId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        using var content = JsonContent.Create(new
        {
            id = invoiceId,
            invoiceNumber = "INV-2026-00001",
            issueDate = DateTime.UtcNow,
            dueDate = DateTime.UtcNow.AddDays(7),
            status = "Finalized",
            customerId = Guid.NewGuid(),
            customerName = "MALIEV Test Customer",
            customerTaxId = "0999999999999",
            billingAddress = "123 Billing Road",
            subtotal = 1000m,
            taxAmount = 70m,
            withholdingTaxAmount = 0m,
            grandTotal = 1070m,
            currency = "THB",
            createdAt = DateTime.UtcNow,
            createdBy = "creator-1",
            lines = new[]
            {
                new
                {
                    id = lineId,
                    lineNumber = 1,
                    description = "Integration service item",
                    quantity = 2m,
                    unitPrice = 500m,
                    taxRate = 7m,
                    lineTotal = 1070m
                }
            }
        });
        using var handler = MockHttpMessageHandler.Create(HttpStatusCode.OK, content);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://invoice.test")
        };
        var client = new InvoiceServiceClient(httpClient, NullLogger<InvoiceServiceClient>.Instance);

        var invoice = await client.GetInvoiceAsync(invoiceId);

        Assert.NotNull(invoice);
        Assert.Equal(invoiceId, invoice.Id);
        Assert.Equal("123 Billing Road", invoice.CustomerAddress);
        Assert.Equal(1070m, invoice.TotalAmount);
        Assert.Equal(7m, invoice.VatRate);
        Assert.Single(invoice.LineItems);
        Assert.Equal(lineId, invoice.LineItems[0].Id);
        Assert.Equal(1070m, invoice.LineItems[0].LineTotal);
    }
}
