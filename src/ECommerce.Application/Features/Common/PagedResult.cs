namespace ECommerce.Application.Features.Common;

public class PagedResult<T>
{
    public System.Collections.Generic.IReadOnlyList<T> Items { get; set; } = System.Array.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public static PagedResult<T> Create(System.Collections.Generic.IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        => new PagedResult<T> { Items = items, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize };
}
