using Maliev.ReceiptService.Application.Models.Dtos;

namespace Maliev.ReceiptService.Application.Ports;

/// <summary>
/// Client for communicating with the Invoice Service.
/// </summary>
public interface IInvoiceServiceClient
{
    /// <summary>
    /// Retrieves an invoice by its ID.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The invoice DTO if found; otherwise, null.</returns>
    Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a payment by its ID.
    /// </summary>
    /// <param name="paymentId">The payment ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The payment DTO if found; otherwise, null.</returns>
    Task<InvoicePaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
}
