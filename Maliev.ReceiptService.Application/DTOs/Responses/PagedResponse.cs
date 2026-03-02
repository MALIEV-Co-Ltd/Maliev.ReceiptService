namespace Maliev.ReceiptService.Application.DTOs.Responses;

public class PagedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}

public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
