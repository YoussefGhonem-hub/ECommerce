using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace ECommerce.Application.Common;

public class PagedResult<T>
{
    public System.Collections.Generic.IReadOnlyList<T> Items { get; set; } = System.Array.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public static PagedResult<T> Create(System.Collections.Generic.IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        => new PagedResult<T> { Items = items, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize };
}

public static class PagedResultExtensions
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;

    private static int NormalizePage(int page) => page <= 0 ? DefaultPage : page;
    private static int NormalizeSize(int size) => size <= 0 ? DefaultPageSize : size;

    public static int CalculateSkip(int pageNumber, int pageSize)
        => (NormalizePage(pageNumber) - 1) * NormalizeSize(pageSize);

    public static int CalculateTake(int pageSize)
        => NormalizeSize(pageSize);

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = NormalizePage(pageNumber);
        pageSize = NormalizeSize(pageSize);

        if (query.Provider is IAsyncQueryProvider)
        {
            // EF Core async path
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<T>.Create(items, totalCount, pageNumber, pageSize);
        }
        else
        {
            // Fallback for in-memory/non-EF providers
            var totalCount = query.Count();
            var items = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return PagedResult<T>.Create(items, totalCount, pageNumber, pageSize);
        }
    }

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PaginationParams paging,
        CancellationToken cancellationToken = default)
    {
        if (paging is null)
            return await query.ToPagedResultAsync(DefaultPage, DefaultPageSize, cancellationToken);

        return await query.ToPagedResultAsync(paging.PageNumber, paging.PageSize, cancellationToken);
    }
}