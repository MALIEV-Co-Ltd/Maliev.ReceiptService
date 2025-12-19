using Maliev.ReceiptService.Api.Models.Dtos;

namespace Maliev.ReceiptService.Api.Services;

public interface IInvoiceServiceClient
{
    Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
