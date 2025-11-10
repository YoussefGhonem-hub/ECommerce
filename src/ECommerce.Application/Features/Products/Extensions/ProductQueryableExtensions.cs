using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Products.Extensions;

public static class ProductQueryableExtensions
{
    public static IQueryable<Product> FilterByCategory(this IQueryable<Product> query, Guid? categoryId)
    {
        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            query = query.Where(p => p.CategoryId == categoryId.Value);
        return query;
    }

    public static IQueryable<Product> FilterByPrice(this IQueryable<Product> query, decimal? minPrice, decimal? maxPrice)
    {
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);
        return query;
    }

    public static IQueryable<Product> FilterByColor(this IQueryable<Product> query, string? color)
    {
        if (!string.IsNullOrWhiteSpace(color))
            query = query.Where(p => p.Color != null && p.Color == color);
        return query;
    }

    public static IQueryable<Product> OrderByBestRating(this IQueryable<Product> query, bool bestRating)
    {
        if (bestRating)
            query = query.OrderByDescending(p => p.AverageRating);
        return query;
    }
}
