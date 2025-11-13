using ECommerce.Domain.Entities;

namespace ECommerce.Application.Products.Queries.GetProducts;

public static class QueryableProductExtensions
{
    public static IQueryable<Product> ApplyFilters(this IQueryable<Product> query, GetProductsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.NameAr.Contains(term) ||
                p.NameEn.Contains(term) ||
                p.SKU.Contains(term) ||
                (p.Brand != null && p.Brand.Contains(term)));
        }

        if (request.CategoryId.HasValue)
        {
            var id = request.CategoryId.Value;
            query = query.Where(p => p.CategoryId == id);
        }

        // Price filters
        if (request.Price.HasValue)
        {
            var price = request.Price.Value;
            query = query.Where(p => p.Price == price);
        }
        else
        {
            var min = request.MinPrice;
            var max = request.MaxPrice;

            if (min.HasValue && max.HasValue)
            {
                // auto-correct swapped bounds
                if (min > max)
                    (min, max) = (max, min);

                query = query.Where(p => p.Price >= min.Value && p.Price <= max.Value);
            }
            else if (min.HasValue)
            {
                query = query.Where(p => p.Price >= min.Value);
            }
            else if (max.HasValue)
            {
                query = query.Where(p => p.Price <= max.Value);
            }
        }

        return query;
    }

    // Default sorting: newest first
    public static IOrderedQueryable<Product> ApplySorting(this IQueryable<Product> query)
        => query.OrderByDescending(p => p.CreatedDate);

    // Extended sorting with featured + hyphen/underscore synonyms for price    
    public static IOrderedQueryable<Product> ApplySorting(this IQueryable<Product> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderByDescending(p => p.CreatedDate);

        // Normalize: trim, lower, unify underscores to hyphens
        var key = sort.Trim().ToLowerInvariant().Replace('_', '-');

        return key switch
        {
            // Featured: assumes a boolean property IsFeatured (change to p.Featured if your model uses that name)
            "featured" or "isfeatured" =>
                query.OrderByDescending(p => p.CreatedDate)
                     .ThenByDescending(p => p.CreatedDate),

            // Created (ascending typo support)
            "created-as" or "created-asc" =>
                query.OrderBy(p => p.CreatedDate),

            // Price asc / desc (support both styles)
            "price-asc" =>
                query.OrderBy(p => p.Price),
            "price-desc" =>
                query.OrderByDescending(p => p.Price),

            // Name English asc / desc
            "name-en-asc" =>
                query.OrderBy(p => p.NameEn),
            "name-en-desc" =>
                query.OrderByDescending(p => p.NameEn),

            _ => query.OrderByDescending(p => p.CreatedDate)
        };
    }
}