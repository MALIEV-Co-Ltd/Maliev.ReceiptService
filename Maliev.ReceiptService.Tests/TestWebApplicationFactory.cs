using Maliev.ReceiptService.Data.Data;
using Maliev.ReceiptService.Tests.Testing;
using WireMock.Server;
using WireMock.RequestBuilders;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics.CodeAnalysis;
using MassTransit;
using WireMockResponse = WireMock.ResponseBuilders.Response;

namespace Maliev.ReceiptService.Tests;

public class TestWebApplicationFactory : BaseIntegrationTestFactory<Program, ReceiptDbContext>
{
    private readonly WireMockServer _invoiceServiceMock;

    public WireMockServer InvoiceServiceMock => _invoiceServiceMock;

    public TestWebApplicationFactory()
    {
        // Initialize WireMock servers for external services
        _invoiceServiceMock = WireMockServer.Start();

        // Set up default WireMock responses for external services
        SetupInvoiceServiceMockResponses();
        SetupIAMServiceMockResponses();
    }

    protected override void ConfigureEnvironmentVariables()
    {
        base.ConfigureEnvironmentVariables();
        // Environment variables are still set for non-web components that might read them
        Environment.SetEnvironmentVariable("Services__InvoiceService__BaseUrl", $"{_invoiceServiceMock.Urls[0]}/v1/");
        Environment.SetEnvironmentVariable("Services__IAMService__BaseUrl", _invoiceServiceMock.Urls[0]);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Configure HttpClient base URL for InvoiceService
        // Using builder.UseSetting is more reliable than Environment.SetEnvironmentVariable
        builder.UseSetting("Services:InvoiceService:BaseUrl", $"{_invoiceServiceMock.Urls[0]}/v1/");
        builder.UseSetting("Services:IAMService:BaseUrl", _invoiceServiceMock.Urls[0]);
    }

    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        base.ConfigureAdditionalServices(services);

        // Add permission-based authorization infrastructure for tests
        services.AddHttpContextAccessor();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider,
                              Maliev.Aspire.ServiceDefaults.Authorization.PermissionAuthorizationPolicyProvider>();
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
                           Maliev.Aspire.ServiceDefaults.Authorization.PermissionAuthorizationHandler>();
        services.AddAuthorizationBuilder();
    }

    private void SetupInvoiceServiceMockResponses()
    {
        if (_invoiceServiceMock == null) return;

        // Non-existent invoice (all zeros)
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/00000000-0000-0000-0000-000000000000").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    errorCode = "INVOICE_NOT_FOUND",
                    message = "Invoice not found"
                })));

        // Service unavailable simulation (for timeout/failure tests)
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/00000000-0000-0000-0000-000000000001").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(503)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    errorCode = "SERVICE_UNAVAILABLE",
                    message = "Invoice service is temporarily unavailable"
                })));

        // Invoice with missing tax fields (for tax validation tests)
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/11111111-1111-1111-1111-111111111111").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    invoiceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    invoiceNumber = "INV-TAX-FAIL",
                    customerId = Guid.NewGuid(),
                    customerName = "Test Customer No Tax",
                    customerTaxId = (string?)null,  // Missing tax ID - should fail validation
                    subtotal = 1000.00m,
                    vatRate = 0.0m,  // Invalid VAT rate - should be 7%
                    vatAmount = 0.00m,
                    totalAmount = 1000.00m,
                    status = "Approved",
                    issuedDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    lineItems = new[]
                    {
                        new
                        {
                            lineNumber = 1,
                            description = "Test Product",
                            quantity = 1,
                            unitPrice = 1000.00m,
                            taxRate = 0.0m,
                            lineTotal = 1000.00m
                        }
                    }
                })));

        // Invoice with invalid VAT rate (22222222...)
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/22222222-2222-2222-2222-222222222222").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    invoiceNumber = "INV-INVALID-VAT",
                    customerId = Guid.NewGuid(),
                    customerName = "Test Customer",
                    customerTaxId = "1234567890123",
                    subtotal = 1000.00m,
                    vatRate = 5.0m,  // Invalid VAT rate - should be 7%
                    taxAmount = 50.00m,
                    totalAmount = 1050.00m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    lineItems = Array.Empty<object>()
                })));

        // Invoice with negative withholding tax rate
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/33333333-3333-3333-3333-333333333333").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    invoiceNumber = "INV-NEG-WHT",
                    customerId = Guid.NewGuid(),
                    customerName = "Test Customer",
                    customerTaxId = "1234567890123",
                    subtotal = 1000.00m,
                    vatRate = 7.0m,
                    taxAmount = 70.00m,
                    withholdingTaxRate = -1.0m,  // Negative withholding tax
                    withholdingTaxAmount = -10.00m,
                    totalAmount = 1060.00m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    lineItems = Array.Empty<object>()
                })));

        // Invoice with excessive withholding tax rate (> 100%)
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/44444444-4444-4444-4444-444444444444").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    invoiceNumber = "INV-EXCESS-WHT",
                    customerId = Guid.NewGuid(),
                    customerName = "Test Customer",
                    customerTaxId = "1234567890123",
                    subtotal = 1000.00m,
                    vatRate = 7.0m,
                    taxAmount = 70.00m,
                    withholdingTaxRate = 150.0m,  // Excessive withholding tax
                    withholdingTaxAmount = 1500.00m,
                    totalAmount = -430.00m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    lineItems = Array.Empty<object>()
                })));

        // Invoice with multiple tax errors
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/55555555-5555-5555-5555-555555555555").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    invoiceNumber = "INV-MULTI-ERR",
                    customerId = Guid.NewGuid(),
                    customerName = "Test Customer",
                    customerTaxId = "",  // Empty tax ID
                    subtotal = 1000.00m,
                    vatRate = 5.0m,  // Wrong VAT rate
                    taxAmount = 50.00m,
                    withholdingTaxRate = -5.0m,  // Negative withholding tax
                    withholdingTaxAmount = -50.00m,
                    totalAmount = 1000.00m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    lineItems = Array.Empty<object>()
                })));

        // Invoice for testing invalid tax (88888888...)
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/88888888-8888-8888-8888-888888888888").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    invoiceId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    invoiceNumber = "INV-INVALID",
                    customerId = Guid.NewGuid(),
                    customerName = "Test Customer",
                    customerTaxId = (string?)null,
                    subtotal = 1000.00m,
                    vatRate = 0.0m,
                    vatAmount = 0.00m,
                    totalAmount = 1000.00m,
                    status = "Approved",
                    issuedDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    lineItems = new[]
                    {
                        new
                        {
                            lineNumber = 1,
                            description = "Test Product",
                            quantity = 1,
                            unitPrice = 1000.00m,
                            taxRate = 0.0m,
                            lineTotal = 1000.00m
                        }
                    }
                })));

        // Invoice for testing correlation ID in error (99999999...)
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/99999999-9999-9999-9999-999999999999").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    invoiceId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    invoiceNumber = "INV-CORR-TEST",
                    customerId = Guid.NewGuid(),
                    customerName = "Test Customer",
                    customerTaxId = (string?)null,
                    subtotal = 1000.00m,
                    vatRate = 0.0m,
                    vatAmount = 0.00m,
                    totalAmount = 1000.00m,
                    status = "Approved",
                    issuedDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    lineItems = new[]
                    {
                        new
                        {
                            lineNumber = 1,
                            description = "Test Product",
                            quantity = 1,
                            unitPrice = 1000.00m,
                            taxRate = 0.0m,
                            lineTotal = 1000.00m
                        }
                    }
                })));

        // Invoice for testing PDF event not published (bbbb0000...)
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/bbbb0000-0000-0000-0000-000000000001").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    invoiceId = Guid.Parse("bbbb0000-0000-0000-0000-000000000001"),
                    invoiceNumber = "INV-NO-PDF",
                    customerId = Guid.NewGuid(),
                    customerName = "Test Customer",
                    customerTaxId = (string?)null,
                    subtotal = 1000.00m,
                    vatRate = 0.0m,
                    vatAmount = 0.00m,
                    totalAmount = 1000.00m,
                    status = "Approved",
                    issuedDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    lineItems = new[]
                    {
                        new
                        {
                            lineNumber = 1,
                            description = "Test Product",
                            quantity = 1,
                            unitPrice = 1000.00m,
                            taxRate = 0.0m,
                            lineTotal = 1000.00m
                        }
                    }
                })));

        // Invoice with empty tax ID string (cccc0000...)
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/cccc0000-0000-0000-0000-000000000001").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    invoiceId = Guid.Parse("cccc0000-0000-0000-0000-000000000001"),
                    invoiceNumber = "INV-EMPTY-TAX",
                    customerId = Guid.NewGuid(),
                    customerName = "Test Customer",
                    customerTaxId = "",  // Empty string (not null)
                    subtotal = 1000.00m,
                    vatRate = 0.0m,
                    vatAmount = 0.00m,
                    totalAmount = 1000.00m,
                    status = "Approved",
                    issuedDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    lineItems = new[]
                    {
                        new
                        {
                            lineNumber = 1,
                            description = "Test Product",
                            quantity = 1,
                            unitPrice = 1000.00m,
                            taxRate = 0.0m,
                            lineTotal = 1000.00m
                        }
                    }
                })));

        // Invoices with valid withholding tax rates (aaaa0000 series)
        var validWithholdingRates = new[]
        {
            ("aaaa0000-0000-0000-0000-000000000001", 0.0m),
            ("aaaa0000-0000-0000-0000-000000000002", 1.0m),
            ("aaaa0000-0000-0000-0000-000000000003", 3.0m),
            ("aaaa0000-0000-0000-0000-000000000004", 5.0m),
            ("aaaa0000-0000-0000-0000-000000000005", 10.0m)
        };

        foreach (var (invoiceIdStr, withholdingRate) in validWithholdingRates)
        {
            var subtotal = 1000.00m;
            var vatAmount = 70.00m;
            var withholdingAmount = subtotal * withholdingRate / 100;
            var totalAmount = subtotal + vatAmount - withholdingAmount;

            _invoiceServiceMock
                .Given(Request.Create().WithPath($"/v1/invoices/{invoiceIdStr}").UsingGet())
                .AtPriority(1)
                .RespondWith(WireMockResponse.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(new
                    {
                        id = Guid.Parse(invoiceIdStr),
                        invoiceNumber = $"INV-WHT-{withholdingRate}",
                        customerId = Guid.NewGuid(),
                        customerName = "Test Customer",
                        customerTaxId = "1234567890123",
                        subtotal = subtotal,
                        vatRate = 7.0m,
                        taxAmount = vatAmount,
                        withholdingTaxRate = withholdingRate,
                        withholdingTaxAmount = withholdingAmount,
                        totalAmount = totalAmount,
                        status = "Approved",
                        issueDate = DateTime.UtcNow,
                        dueDate = DateTime.UtcNow.AddDays(30),
                        lineItems = Array.Empty<object>()
                    })));
        }

        // Default response for all other invoices - returns a valid invoice with proper Thai tax fields
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/*").UsingGet())
            .AtPriority(10)  // Lower priority than specific mocks
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    invoiceId = Guid.NewGuid(),
                    invoiceNumber = "INV-001",
                    customerId = Guid.NewGuid(),
                    customerName = "Test Customer",
                    customerTaxId = "1234567890123",  // Thai Tax ID (13 digits)
                    subtotal = 1000.00m,
                    vatRate = 7.0m,  // Thailand standard VAT rate is 7%
                    vatAmount = 70.00m,
                    totalAmount = 1070.00m,
                    status = "Approved",
                    issuedDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    lineItems = new[]
                    {
                        new
                        {
                            lineNumber = 1,
                            description = "Test Product",
                            quantity = 1,
                            unitPrice = 1000.00m,
                            taxRate = 7.0m,
                            lineTotal = 1070.00m
                        }
                    }
                })));

        // Split Invoice stubs for SplitInvoiceReceiptTests
        // Invoice 550e8400-e29b-41d4-a716-446655440030 with 3 segments
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/550e8400-e29b-41d4-a716-446655440030").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("550e8400-e29b-41d4-a716-446655440030"),
                    invoiceNumber = "INV-SPLIT-001",
                    customerId = Guid.Parse("c0000000-0000-0000-0000-000000000001"),
                    customerName = "Split Invoice Customer 1",
                    customerTaxId = "1234567890123",
                    customerAddress = "123 Test St",
                    subtotal = 1000.00m,
                    taxAmount = 70.00m,
                    totalAmount = 1000.00m,
                    currency = "THB",
                    sellerTaxId = "9876543210987",
                    vatRate = 7.0m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow.AddDays(-5),
                    dueDate = DateTime.UtcNow.AddDays(25),
                    totalPaidAmount = 0m,
                    remainingBalance = 1000.00m,
                    paymentStatus = "Unpaid",
                    createdAt = DateTime.UtcNow.AddDays(-10),
                    createdBy = "system",
                    segments = new[]
                    {
                        new
                        {
                            segmentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            segmentName = "Segment 1",
                            segmentAmount = 500.00m,
                            segmentTaxRate = 7.0m,
                            segmentTotal = 535.00m,
                            lineItemIds = new[] { Guid.NewGuid() }
                        },
                        new
                        {
                            segmentId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                            segmentName = "Segment 2",
                            segmentAmount = 300.00m,
                            segmentTaxRate = 7.0m,
                            segmentTotal = 321.00m,
                            lineItemIds = new[] { Guid.NewGuid() }
                        },
                        new
                        {
                            segmentId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                            segmentName = "Segment 3",
                            segmentAmount = 200.00m,
                            segmentTaxRate = 7.0m,
                            segmentTotal = 214.00m,
                            lineItemIds = new[] { Guid.NewGuid() }
                        }
                    },
                    lineItems = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 1,
                            description = "Product A",
                            quantity = 1m,
                            unitPrice = 500.00m,
                            taxRate = 7.0m,
                            lineTotal = 535.00m
                        },
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 2,
                            description = "Product B",
                            quantity = 1m,
                            unitPrice = 300.00m,
                            taxRate = 7.0m,
                            lineTotal = 321.00m
                        },
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 3,
                            description = "Product C",
                            quantity = 1m,
                            unitPrice = 200.00m,
                            taxRate = 7.0m,
                            lineTotal = 214.00m
                        }
                    }
                })));

        // Invoice 550e8400-e29b-41d4-a716-446655440031 - multiple segments test
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/550e8400-e29b-41d4-a716-446655440031").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("550e8400-e29b-41d4-a716-446655440031"),
                    invoiceNumber = "INV-SPLIT-002",
                    customerId = Guid.Parse("c0000000-0000-0000-0000-000000000002"),
                    customerName = "Split Invoice Customer 2",
                    customerTaxId = "1234567890123",
                    customerAddress = "456 Test Ave",
                    subtotal = 1500.00m,
                    taxAmount = 105.00m,
                    totalAmount = 1605.00m,
                    currency = "THB",
                    sellerTaxId = "9876543210987",
                    vatRate = 7.0m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow.AddDays(-5),
                    dueDate = DateTime.UtcNow.AddDays(25),
                    totalPaidAmount = 0m,
                    remainingBalance = 1605.00m,
                    paymentStatus = "Unpaid",
                    createdAt = DateTime.UtcNow.AddDays(-10),
                    createdBy = "system",
                    segments = new[]
                    {
                        new
                        {
                            segmentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            segmentName = "Department A",
                            segmentAmount = 1000.00m,
                            segmentTaxRate = 7.0m,
                            segmentTotal = 1070.00m,
                            lineItemIds = new[] { Guid.NewGuid() }
                        },
                        new
                        {
                            segmentId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                            segmentName = "Department B",
                            segmentAmount = 500.00m,
                            segmentTaxRate = 7.0m,
                            segmentTotal = 535.00m,
                            lineItemIds = new[] { Guid.NewGuid() }
                        }
                    },
                    lineItems = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 1,
                            description = "Service X",
                            quantity = 1m,
                            unitPrice = 1000.00m,
                            taxRate = 7.0m,
                            lineTotal = 1070.00m
                        },
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 2,
                            description = "Service Y",
                            quantity = 1m,
                            unitPrice = 500.00m,
                            taxRate = 7.0m,
                            lineTotal = 535.00m
                        }
                    }
                })));

        // Invoice 550e8400-e29b-41d4-a716-446655440032 - segment-specific tax test
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/550e8400-e29b-41d4-a716-446655440032").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("550e8400-e29b-41d4-a716-446655440032"),
                    invoiceNumber = "INV-SPLIT-003",
                    customerId = Guid.Parse("c0000000-0000-0000-0000-000000000003"),
                    customerName = "Split Invoice Customer 3",
                    customerTaxId = "1234567890123",
                    customerAddress = "789 Test Blvd",
                    subtotal = 1000.00m,
                    taxAmount = 70.00m,
                    totalAmount = 1070.00m,
                    currency = "THB",
                    sellerTaxId = "9876543210987",
                    vatRate = 7.0m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow.AddDays(-3),
                    dueDate = DateTime.UtcNow.AddDays(27),
                    totalPaidAmount = 0m,
                    remainingBalance = 1070.00m,
                    paymentStatus = "Unpaid",
                    createdAt = DateTime.UtcNow.AddDays(-7),
                    createdBy = "system",
                    segments = new[]
                    {
                        new
                        {
                            segmentId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                            segmentName = "Department C",
                            segmentAmount = 1000.00m,
                            segmentTaxRate = 7.0m,
                            segmentTotal = 1070.00m,
                            lineItemIds = new[] { Guid.NewGuid() }
                        }
                    },
                    lineItems = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 1,
                            description = "Consulting Services",
                            quantity = 1m,
                            unitPrice = 1000.00m,
                            taxRate = 7.0m,
                            lineTotal = 1070.00m
                        }
                    }
                })));

        // Invoice 550e8400-e29b-41d4-a716-446655440033 - withholding tax segment test
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/550e8400-e29b-41d4-a716-446655440033").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("550e8400-e29b-41d4-a716-446655440033"),
                    invoiceNumber = "INV-SPLIT-004",
                    customerId = Guid.Parse("c0000000-0000-0000-0000-000000000004"),
                    customerName = "Split Invoice Customer 4",
                    customerTaxId = "1234567890123",
                    customerAddress = "321 Test Lane",
                    subtotal = 1000.00m,
                    taxAmount = 70.00m,
                    withholdingTaxAmount = 30.00m,
                    totalAmount = 1040.00m,
                    currency = "THB",
                    sellerTaxId = "9876543210987",
                    vatRate = 7.0m,
                    withholdingTaxRate = 3.0m,
                    withholdingTaxType = "Professional",
                    status = "Approved",
                    issueDate = DateTime.UtcNow.AddDays(-2),
                    dueDate = DateTime.UtcNow.AddDays(28),
                    totalPaidAmount = 0m,
                    remainingBalance = 1040.00m,
                    paymentStatus = "Unpaid",
                    createdAt = DateTime.UtcNow.AddDays(-5),
                    createdBy = "system",
                    segments = new[]
                    {
                        new
                        {
                            segmentId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                            segmentName = "Professional Services",
                            segmentAmount = 1000.00m,
                            segmentTaxRate = 7.0m,
                            segmentTotal = 1040.00m,
                            lineItemIds = new[] { Guid.NewGuid() }
                        }
                    },
                    lineItems = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 1,
                            description = "Professional Services",
                            quantity = 1m,
                            unitPrice = 1000.00m,
                            taxRate = 7.0m,
                            lineTotal = 1040.00m
                        }
                    }
                })));

        // Invoice 550e8400-e29b-41d4-a716-446655440034 - GetReceipts_FilterByInvoiceId_ShowsSegmentReceiptingStatus
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/550e8400-e29b-41d4-a716-446655440034").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("550e8400-e29b-41d4-a716-446655440034"),
                    invoiceNumber = "INV-SPLIT-034",
                    customerId = Guid.NewGuid(),
                    customerName = "Split Invoice Customer 034",
                    customerTaxId = "1234567890123",
                    customerAddress = "Test Address",
                    subtotal = 2000.00m,
                    taxAmount = 140.00m,
                    totalAmount = 2140.00m,
                    currency = "THB",
                    sellerTaxId = "9876543210987",
                    vatRate = 7.0m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow.AddDays(-1),
                    dueDate = DateTime.UtcNow.AddDays(29),
                    totalPaidAmount = 0m,
                    remainingBalance = 2140.00m,
                    paymentStatus = "Unpaid",
                    createdAt = DateTime.UtcNow.AddDays(-3),
                    createdBy = "system",
                    Segments = new[]
                    {
                        new
                        {
                            SegmentId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                            SegmentName = "Segment A",
                            SegmentAmount = 1000.00m,
                            SegmentTaxRate = 7.0m,
                            SegmentTotal = 1070.00m,
                            LineItemIds = new[] { Guid.NewGuid() }
                        },
                        new
                        {
                            SegmentId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                            SegmentName = "Segment B",
                            SegmentAmount = 1000.00m,
                            SegmentTaxRate = 7.0m,
                            SegmentTotal = 1070.00m,
                            LineItemIds = new[] { Guid.NewGuid() }
                        }
                    },
                    lineItems = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 1,
                            description = "Item 1",
                            quantity = 1m,
                            unitPrice = 1000.00m,
                            taxRate = 7.0m,
                            lineTotal = 1070.00m
                        },
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 2,
                            description = "Item 2",
                            quantity = 1m,
                            unitPrice = 1000.00m,
                            taxRate = 7.0m,
                            lineTotal = 1070.00m
                        }
                    }
                })));

        // Invoice 550e8400-e29b-41d4-a716-446655440035 - GetReceipts_FilterBySegmentId_ReturnsOnlySegmentReceipts
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/550e8400-e29b-41d4-a716-446655440035").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("550e8400-e29b-41d4-a716-446655440035"),
                    invoiceNumber = "INV-SPLIT-035",
                    customerId = Guid.NewGuid(),
                    customerName = "Split Invoice Customer 035",
                    customerTaxId = "1234567890123",
                    customerAddress = "Test Address",
                    subtotal = 2000.00m,
                    taxAmount = 140.00m,
                    totalAmount = 2140.00m,
                    currency = "THB",
                    sellerTaxId = "9876543210987",
                    vatRate = 7.0m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow.AddDays(-1),
                    dueDate = DateTime.UtcNow.AddDays(29),
                    totalPaidAmount = 0m,
                    remainingBalance = 2140.00m,
                    paymentStatus = "Unpaid",
                    createdAt = DateTime.UtcNow.AddDays(-3),
                    createdBy = "system",
                    Segments = new[]
                    {
                        new
                        {
                            SegmentId = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                            SegmentName = "Segment A",
                            SegmentAmount = 1000.00m,
                            SegmentTaxRate = 7.0m,
                            SegmentTotal = 1070.00m,
                            LineItemIds = new[] { Guid.NewGuid() }
                        },
                        new
                        {
                            SegmentId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                            SegmentName = "Segment B",
                            SegmentAmount = 1000.00m,
                            SegmentTaxRate = 7.0m,
                            SegmentTotal = 1070.00m,
                            LineItemIds = new[] { Guid.NewGuid() }
                        }
                    },
                    lineItems = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 1,
                            description = "Item 1",
                            quantity = 1m,
                            unitPrice = 1000.00m,
                            taxRate = 7.0m,
                            lineTotal = 1070.00m
                        },
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 2,
                            description = "Item 2",
                            quantity = 1m,
                            unitPrice = 1000.00m,
                            taxRate = 7.0m,
                            lineTotal = 1070.00m
                        }
                    }
                })));

        // Invoice 550e8400-e29b-41d4-a716-446655440036 - GetReceipts_ForSplitInvoice_ProvidesSegmentStatusOverview (3 segments)
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/550e8400-e29b-41d4-a716-446655440036").UsingGet())
            .AtPriority(1)
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.Parse("550e8400-e29b-41d4-a716-446655440036"),
                    invoiceNumber = "INV-SPLIT-036",
                    customerId = Guid.NewGuid(),
                    customerName = "Split Invoice Customer 036",
                    customerTaxId = "1234567890123",
                    customerAddress = "Test Address",
                    subtotal = 3000.00m,
                    taxAmount = 210.00m,
                    totalAmount = 3210.00m,
                    currency = "THB",
                    sellerTaxId = "9876543210987",
                    vatRate = 7.0m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow.AddDays(-1),
                    dueDate = DateTime.UtcNow.AddDays(29),
                    totalPaidAmount = 0m,
                    remainingBalance = 3210.00m,
                    paymentStatus = "Unpaid",
                    createdAt = DateTime.UtcNow.AddDays(-3),
                    createdBy = "system",
                    Segments = new[]
                    {
                        new
                        {
                            SegmentId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                            SegmentName = "Segment 1",
                            SegmentAmount = 500.00m,
                            SegmentTaxRate = 7.0m,
                            SegmentTotal = 535.00m,
                            LineItemIds = new[] { Guid.NewGuid() }
                        },
                        new
                        {
                            SegmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                            SegmentName = "Segment 2",
                            SegmentAmount = 300.00m,
                            SegmentTaxRate = 7.0m,
                            SegmentTotal = 321.00m,
                            LineItemIds = new[] { Guid.NewGuid() }
                        },
                        new
                        {
                            SegmentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                            SegmentName = "Segment 3",
                            SegmentAmount = 200.00m,
                            SegmentTaxRate = 7.0m,
                            SegmentTotal = 214.00m,
                            LineItemIds = new[] { Guid.NewGuid() }
                        }
                    },
                    lineItems = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 1,
                            description = "Item 1",
                            quantity = 1m,
                            unitPrice = 500.00m,
                            taxRate = 7.0m,
                            lineTotal = 535.00m
                        },
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 2,
                            description = "Item 2",
                            quantity = 1m,
                            unitPrice = 300.00m,
                            taxRate = 7.0m,
                            lineTotal = 321.00m
                        },
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 3,
                            description = "Item 3",
                            quantity = 1m,
                            unitPrice = 200.00m,
                            taxRate = 7.0m,
                            lineTotal = 214.00m
                        }
                    }
                })));

        // Catch-all stub for any other invoice ID
        // Returns a valid generic invoice with 2140.00 total (2000 + 7% VAT) to accommodate most tests
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/v1/invoices/*").UsingGet())
            .AtPriority(999) // Lowest priority - only matches if no specific stub matched
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(new
                {
                    id = Guid.NewGuid(),
                    invoiceNumber = "INV-GENERIC",
                    customerId = Guid.NewGuid(),
                    customerName = "Generic Test Customer",
                    customerTaxId = "1234567890123",
                    customerAddress = "123 Test Street, Bangkok 10100",
                    sellerTaxId = "9876543210987",
                    currency = "THB",
                    subtotal = 2000.00m,
                    vatRate = 7.0m,
                    taxAmount = 140.00m,
                    withholdingTaxRate = 0.0m,
                    withholdingTaxAmount = 0.0m,
                    withholdingTaxType = "None",
                    totalAmount = 2140.00m,
                    status = "Approved",
                    issueDate = DateTime.UtcNow,
                    dueDate = DateTime.UtcNow.AddDays(30),
                    totalPaidAmount = 0m,
                    remainingBalance = 2140.00m,
                    paymentStatus = "Unpaid",
                    createdAt = DateTime.UtcNow,
                    createdBy = "system",
                    lineItems = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            lineNumber = 1,
                            description = "Generic Line Item",
                            quantity = 20m,
                            unitPrice = 100.00m,
                            taxRate = 7.0m,
                            lineTotal = 2140.00m
                        }
                    }
                })));
    }

    /// <summary>
    /// Creates an authenticated HTTP client with all receipt permissions for testing.
    /// This is a convenience method for tests that need full access to receipt operations.
    /// </summary>
    public HttpClient CreateAuthenticatedClientWithAllPermissions(string userId = "test-user")
    {
        var allPermissions = new[]
        {
            "receipt.receipts.create",
            "receipt.receipts.read",
            "receipt.receipts.update",
            "receipt.receipts.void",
            "receipt.receipts.send",
            "receipt.receipts.query",
            "receipt.receipts.export",
            "receipt.partial-payments.create",
            "receipt.partial-payments.read",
            "receipt.partial-payments.manage",
            "receipt.audits.read",
            "receipt.audits.export"
        };

        var token = CreateTestJwtToken(userId, roles: null, permissions: allPermissions);
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    private void SetupIAMServiceMockResponses()
    {
        if (_invoiceServiceMock == null) return;

        // Mock IAM permission registration
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/iam/v1/permissions/register").UsingPost())
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"status\":\"success\"}"));

        // Mock IAM role registration
        _invoiceServiceMock
            .Given(Request.Create().WithPath("/iam/v1/roles/register").UsingPost())
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"status\":\"success\"}"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _invoiceServiceMock?.Stop();
            _invoiceServiceMock?.Dispose();
        }
        base.Dispose(disposing);
    }
}
