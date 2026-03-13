namespace Maliev.ReceiptService.Application.Models.Responses;

/// <summary>
/// Generic paginated response wrapper
/// Per contracts/receipts-api.yaml
/// </summary>
public class PagedResponse<T>
{
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public List<T> Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the pagination metadata.
    /// </summary>
    public PaginationMetadata Pagination { get; set; } = new();
}
