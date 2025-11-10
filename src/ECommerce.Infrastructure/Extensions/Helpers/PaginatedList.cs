namespace ECommerce.Infrastructure.Extensions.Helpers;

public class PaginatedList<T> where T : class
{
    public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber + 1;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        LastItemOnPage = Math.Min(PageNumber * PageSize, TotalCount);
        FirstItemOnPage = (PageNumber - 1) * PageSize + 1;
        IsLastPage = PageNumber == TotalPages;
        IsFirstPage = PageNumber == 1;
        Items!.AddRange(items);
    }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public int FirstItemOnPage { get; set; }

    public int LastItemOnPage { get; set; }

    public bool IsFirstPage { get; set; }
    public bool IsLastPage { get; set; }

    public List<T> Items { get; set; } = [];
}