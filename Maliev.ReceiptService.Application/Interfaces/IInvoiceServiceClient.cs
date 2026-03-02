using Maliev.ReceiptService.Application.DTOs.Responses;

namespace Maliev.ReceiptService.Application.Interfaces;

public interface IInvoiceServiceClient
{
    Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
