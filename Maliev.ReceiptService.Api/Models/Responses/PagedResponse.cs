namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Generic paginated response wrapper
/// Per contracts/receipts-api.yaml
/// </summary>
public class PagedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}
