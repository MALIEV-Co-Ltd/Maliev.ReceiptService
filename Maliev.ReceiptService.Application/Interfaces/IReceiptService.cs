using Maliev.ReceiptService.Application.DTOs.Requests;
using Maliev.ReceiptService.Application.DTOs.Responses;
using Maliev.ReceiptService.Domain.Enums;

namespace Maliev.ReceiptService.Application.Interfaces;

public interface IReceiptService
{
    Task<ReceiptResponse> CreateReceiptAsync(
        CreateReceiptRequest request,
        string staffId,
        Guid correlationId);

    Task<ReceiptResponse?> GetReceiptByIdAsync(Guid id);

    Task<PagedResponse<ReceiptResponse>> QueryReceiptsAsync(
        Guid? invoiceId = null,
        ReceiptStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? segmentId = null,
        int page = 1,
        int pageSize = 20,
        string sortBy = "issueDate",
        string sortOrder = "desc");

    Task<ReceiptResponse> VoidReceiptAsync(
        Guid receiptId,
        string reason,
        string staffId,
        Guid correlationId);

    Task<List<AuditEvent>> GetAuditHistoryAsync(Guid receiptId);

    Task<ReceiptResponse> SendReceiptAsync(
        Guid receiptId,
        string destination,
        string channel,
        string staffId,
        Guid correlationId);
}
