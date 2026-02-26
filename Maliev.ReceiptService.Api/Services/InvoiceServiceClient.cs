using Maliev.ReceiptService.Api.Models.Dtos;
using Maliev.ReceiptService.Api.Exceptions;
using System.Text.Json;

namespace Maliev.ReceiptService.Api.Services;

/// <summary>
/// Implementation of the invoice service client.
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

            var response = await _httpClient.GetAsync($"invoices/{invoiceId}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Invoice {InvoiceId} not found", invoiceId);
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                _logger.LogError("Invoice Service is unavailable (503) while fetching invoice {InvoiceId}", invoiceId);
                throw new InvoiceServiceUnavailableException("Invoice Service is temporarily unavailable");
            }

            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var invoice = JsonSerializer.Deserialize<InvoiceDto>(jsonContent, JsonOptions);

            _logger.LogInformation("Successfully fetched invoice {InvoiceId}", invoiceId);

            return invoice;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                                             ex.InnerException is System.Net.Sockets.SocketException ||
                                             ex.InnerException is System.IO.IOException)
        {
            _logger.LogError(ex, "Transient error fetching invoice {InvoiceId} from Invoice Service", invoiceId);
            throw new InvoiceServiceUnavailableException("Invoice Service is temporarily unavailable", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout fetching invoice {InvoiceId} from Invoice Service", invoiceId);
            throw new InvoiceServiceUnavailableException("Invoice Service request timed out", ex);
        }
        catch (Exception ex) when (ex is not InvoiceServiceUnavailableException)
        {
            _logger.LogError(ex, "Error fetching invoice {InvoiceId} from Invoice Service", invoiceId);
            throw;
        }
    }
}
