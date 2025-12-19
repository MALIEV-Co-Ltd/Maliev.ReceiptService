namespace Maliev.ReceiptService.Api.Models.Responses;

/// <summary>
/// Pagination metadata for query responses
/// Per contracts/receipts-api.yaml
/// </summary>
public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
