using ECommerce.Domain.Common;
using ECommerce.Infrastructure.Extensions.Helpers;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECommerce.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> queryable, bool condition, Expression<Func<T, bool>> expression)
        where T : class
        => condition ? queryable.Where(expression) : queryable;

    public static async Task<PaginatedList<T>> PaginateAsync<T>(this IQueryable<T> querable, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        where T : class
    {
        if (pageSize < 0)
        {
            var all = await querable.AsNoTracking().ToListAsync(cancellationToken);
            return new PaginatedList<T>(all, all.Count, pageIndex, pageSize);
        }

        if (pageIndex <= 0) pageIndex = 1;
        var zeroBased = pageIndex - 1;

        var totalCount = await querable.CountAsync(cancellationToken);
        var data = await querable
            .AsNoTracking()
            .Skip(zeroBased * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(data, totalCount, pageIndex, pageSize);
    }

    // Mapster-based pagination (entity -> DTO)
    public static async Task<PaginatedList<TDto>> PaginateAsync<T, TDto>(
        this IQueryable<T> queryable,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
        where T : BaseEntity
        where TDto : class
    {
        if (pageSize < 0)
        {
            // Project entire set in one go
            var allProjected = await queryable
                .AsNoTracking()
                .ProjectToType<TDto>()
                .ToListAsync(cancellationToken);

            return new PaginatedList<TDto>(allProjected, allProjected.Count, pageIndex, pageSize);
        }

        if (pageIndex <= 0) pageIndex = 1;
        var zeroBased = pageIndex - 1;

        // Apply default ordering if not already applied
        // Ensure stable order for pagination
        var ordered = queryable.OrderByDescending(e => e.CreatedDate);

        var totalCount = await ordered.CountAsync(cancellationToken);

        // Perform projection BEFORE materialization so EF translates select
        var pageItems = await ordered
            .Skip(zeroBased * pageSize)
            .Take(pageSize)
            .ProjectToType<TDto>()              // Mapster projection
            .ToListAsync(cancellationToken);

        return new PaginatedList<TDto>(pageItems, totalCount, pageIndex, pageSize);
    }

    // Synchronous Mapster-based pagination
    public static PaginatedList<TDto> Paginate<T, TDto>(
        this IQueryable<T> queryable,
        int pageIndex,
        int pageSize)
        where T : BaseEntity
        where TDto : class
    {
        if (pageSize < 0)
        {
            var allProjected = queryable
                .AsNoTracking()
                .ProjectToType<TDto>()
                .ToList();

            return new PaginatedList<TDto>(allProjected, allProjected.Count, pageIndex, pageSize);
        }

        if (pageIndex <= 0) pageIndex = 1;
        var zeroBased = pageIndex - 1;

        var ordered = queryable.OrderByDescending(e => e.CreatedDate);
        var totalCount = ordered.Count();

        var pageItems = ordered
            .Skip(zeroBased * pageSize)
            .Take(pageSize)
            .ProjectToType<TDto>()
            .ToList();

        return new PaginatedList<TDto>(pageItems, totalCount, pageIndex, pageSize);
    }

    public static PaginatedList<T> Paginate<T>(this IQueryable<T> querable, int pageIndex, int pageSize)
        where T : class
    {
        if (pageSize < 0)
        {
            var all = querable.AsNoTracking().ToList();
            return new PaginatedList<T>(all, all.Count, pageIndex, pageSize);
        }

        if (pageIndex <= 0) pageIndex = 1;
        var zeroBased = pageIndex - 1;

        var totalCount = querable.Count();
        var data = querable
            .AsNoTracking()
            .Skip(zeroBased * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedList<T>(data, totalCount, pageIndex, pageSize);
    }

    public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> source, string? sortBy, bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            sortBy = "CreatedDate";

        var parameter = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(parameter, sortBy);
        var converted = Expression.Convert(property, typeof(object));
        var lambda = Expression.Lambda<Func<T, object>>(converted, parameter);

        return descending ? source.OrderByDescending(lambda) : source.OrderBy(lambda);
    }

    public static IQueryable<T> ApplyDefaultSorting<T>(this IQueryable<T> query) where T : BaseEntity
        => query.OrderByDescending(e => e.CreatedDate);
}
