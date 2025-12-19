using Maliev.ReceiptService.Api.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.ReceiptService.Api.Services;

public class InvoiceServiceClient : IInvoiceServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InvoiceServiceClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InvoiceServiceClient(HttpClient httpClient, ILogger<InvoiceServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching invoice {InvoiceId} from Invoice Service", invoiceId);

            var response = await _httpClient.GetAsync($"invoices/{invoiceId}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Invoice {InvoiceId} not found", invoiceId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var invoice = JsonSerializer.Deserialize<InvoiceDto>(jsonContent, JsonOptions);

            _logger.LogInformation("Successfully fetched invoice {InvoiceId}", invoiceId);

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invoice {InvoiceId} from Invoice Service", invoiceId);
            throw;
        }
    }
}
