using Maliev.ReceiptService.Application.Models.Dtos;
using Maliev.ReceiptService.Application.Ports;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Maliev.ReceiptService.Infrastructure.ExternalServices;

/// <summary>
/// Implementation of the invoice service client.
/// Makes HTTP calls to the external Invoice Service.
/// </summary>
public class InvoiceServiceClient : IInvoiceServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InvoiceServiceClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public InvoiceServiceClient(HttpClient httpClient, ILogger<InvoiceServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves an invoice by its ID.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The invoice DTO if found; otherwise, null.</returns>
    public async Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching invoice {InvoiceId} from Invoice Service", invoiceId);

            var response = await _httpClient.GetAsync($"/invoice/v1/invoices/{invoiceId}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Invoice {InvoiceId} not found", invoiceId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var invoice = JsonSerializer.Deserialize<InvoiceServiceInvoiceResponse>(jsonContent, JsonOptions)?.ToInvoiceDto();

            _logger.LogInformation("Successfully fetched invoice {InvoiceId}", invoiceId);

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invoice {InvoiceId} from Invoice Service", invoiceId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a payment by its ID.
    /// </summary>
    /// <param name="paymentId">The payment ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The payment DTO if found; otherwise, null.</returns>
    public async Task<InvoicePaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching payment {PaymentId} from Invoice Service", paymentId);

            var response = await _httpClient.GetAsync($"/invoice/v1/payments/{paymentId}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Payment {PaymentId} not found", paymentId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var payment = JsonSerializer.Deserialize<InvoiceServicePaymentResponse>(jsonContent, JsonOptions)?.ToInvoicePaymentDto();

            _logger.LogInformation("Successfully fetched payment {PaymentId}", paymentId);

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment {PaymentId} from Invoice Service", paymentId);
            throw;
        }
    }

    private sealed class InvoiceServiceInvoiceResponse
    {
        public Guid Id { get; set; }

        public Guid InvoiceId { get; set; }

        public string? InvoiceNumber { get; set; }

        public DateTime IssueDate { get; set; }

        public DateTime IssuedDate { get; set; }

        public DateTime? DueDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerTaxId { get; set; } = string.Empty;

        public string CustomerAddress { get; set; } = string.Empty;

        public string BillingAddress { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal VatAmount { get; set; }

        public decimal WithholdingTaxAmount { get; set; }

        public decimal VatRate { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal GrandTotal { get; set; }

        public string Currency { get; set; } = "THB";

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public List<InvoiceServiceInvoiceLineResponse> Lines { get; set; } = new();

        public List<InvoiceServiceInvoiceLineResponse> LineItems { get; set; } = new();

        public List<InvoiceServiceInvoiceSegmentResponse> Segments { get; set; } = new();

        public InvoiceDto ToInvoiceDto()
        {
            var lineItems = Lines.Count > 0 ? Lines : LineItems;

            return new InvoiceDto
            {
                Id = Id == Guid.Empty ? InvoiceId : Id,
                InvoiceNumber = InvoiceNumber ?? string.Empty,
                IssueDate = IssueDate == default ? IssuedDate : IssueDate,
                DueDate = DueDate,
                Status = Status,
                CustomerId = CustomerId,
                CustomerName = CustomerName,
                CustomerTaxId = CustomerTaxId,
                CustomerAddress = string.IsNullOrWhiteSpace(BillingAddress) ? CustomerAddress : BillingAddress,
                Subtotal = Subtotal,
                TaxAmount = TaxAmount == 0m ? VatAmount : TaxAmount,
                WithholdingTaxAmount = WithholdingTaxAmount,
                TotalAmount = GrandTotal == 0m ? TotalAmount : GrandTotal,
                Currency = Currency,
                VatRate = ResolveVatRate(lineItems),
                LineItems = lineItems.Select(line => line.ToInvoiceLineItemDto()).ToList(),
                Segments = Segments.Select(segment => segment.ToInvoiceSegmentDto()).ToList(),
                CreatedAt = CreatedAt,
                CreatedBy = CreatedBy ?? string.Empty
            };
        }

        private decimal ResolveVatRate(IReadOnlyCollection<InvoiceServiceInvoiceLineResponse> lineItems)
        {
            if (VatRate > 0)
            {
                return VatRate;
            }

            if (lineItems.Count > 0)
            {
                return lineItems.Max(line => line.TaxRate);
            }

            return Subtotal > 0
                ? Math.Round((TaxAmount == 0m ? VatAmount : TaxAmount) / Subtotal * 100m, 2)
                : 0m;
        }
    }

    private sealed class InvoiceServiceInvoiceLineResponse
    {
        public Guid Id { get; set; }

        public int LineNumber { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TaxRate { get; set; }

        public decimal LineTotal { get; set; }

        public InvoiceLineItemDto ToInvoiceLineItemDto()
        {
            return new InvoiceLineItemDto
            {
                Id = Id,
                LineNumber = LineNumber,
                Description = Description,
                Quantity = Quantity,
                UnitPrice = UnitPrice,
                TaxRate = TaxRate,
                LineTotal = LineTotal
            };
        }
    }

    private sealed class InvoiceServicePaymentResponse
    {
        public Guid Id { get; set; }

        public decimal PaymentAmount { get; set; }

        public DateTime PaymentDate { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public string? ReferenceNumber { get; set; }

        public InvoicePaymentDto ToInvoicePaymentDto()
        {
            return new InvoicePaymentDto
            {
                Id = Id,
                PaymentAmount = PaymentAmount,
                PaymentDate = PaymentDate,
                PaymentMethod = PaymentMethod,
                ReferenceNumber = ReferenceNumber
            };
        }
    }

    private sealed class InvoiceServiceInvoiceSegmentResponse
    {
        public Guid SegmentId { get; set; }

        public string SegmentName { get; set; } = string.Empty;

        public decimal SegmentAmount { get; set; }

        public decimal SegmentTaxRate { get; set; }

        public decimal SegmentTotal { get; set; }

        public List<Guid> LineItemIds { get; set; } = new();

        public InvoiceSegmentDto ToInvoiceSegmentDto()
        {
            return new InvoiceSegmentDto
            {
                SegmentId = SegmentId,
                SegmentName = SegmentName,
                SegmentAmount = SegmentAmount,
                SegmentTaxRate = SegmentTaxRate,
                SegmentTotal = SegmentTotal,
                LineItemIds = LineItemIds
            };
        }
    }
}
