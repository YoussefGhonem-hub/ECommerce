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

    // Existing default (kept for backward compatibility)
    public static IOrderedQueryable<Product> ApplySorting(this IQueryable<Product> query)
        => query.OrderByDescending(p => p.CreatedDate);

    // Existing overload (still used elsewhere if needed)
    public static IOrderedQueryable<Product> ApplySorting(this IQueryable<Product> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderByDescending(p => p.CreatedDate);

        var key = sort.Trim().ToLowerInvariant().Replace('_', '-');

        return key switch
        {
            "featured" or "isfeatured" =>
                query.OrderByDescending(p => p.CreatedDate)
                     .ThenByDescending(p => p.CreatedDate),

            "created-as" or "created-asc" =>
                query.OrderBy(p => p.CreatedDate),

            "price-asc" =>
                query.OrderBy(p => p.Price),
            "price-desc" =>
                query.OrderByDescending(p => p.Price),

            "name-en-asc" =>
                query.OrderBy(p => p.NameEn),
            "name-en-desc" =>
                query.OrderByDescending(p => p.NameEn),

            _ => query.OrderByDescending(p => p.CreatedDate)
        };
    }

    // New overload with wishlist/favorites awareness
    public static IOrderedQueryable<Product> ApplySorting(this IQueryable<Product> query, string? sort, Guid? currentUserId, string? guestId)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderByDescending(p => p.CreatedDate);

        var key = sort.Trim().ToLowerInvariant().Replace('_', '-');
        var isAuthenticated = currentUserId.HasValue;

        // Helper predicate used in ordering
        // EF Core will translate the Any() with the captured variables into proper SQL.
        bool FavoritePredicate(Product p) =>
            p.FavoriteProducts.Any(fp =>
                (isAuthenticated && fp.UserId == currentUserId) ||
                (!isAuthenticated && guestId != null && fp.GuestId == guestId));

        return key switch
        {
            // Wishlist / favorites first (non-favorites later)
            "wishlist" or "wishlist-first" or "favorites" or "favorites-first" or "favorite-first" =>
                query
                    .OrderByDescending(p => p.FavoriteProducts.Any(fp =>
                        (isAuthenticated && fp.UserId == currentUserId) ||
                        (!isAuthenticated && guestId != null && fp.GuestId == guestId)))
                    .ThenByDescending(p => p.CreatedDate),

            // Only favorites first then price desc
            "wishlist-price-desc" =>
                query
                    .OrderByDescending(p => p.FavoriteProducts.Any(fp =>
                        (isAuthenticated && fp.UserId == currentUserId) ||
                        (!isAuthenticated && guestId != null && fp.GuestId == guestId)))
                    .ThenByDescending(p => p.Price),

            // Existing keys reuse prior logic
            "featured" or "isfeatured" =>
                query.OrderByDescending(p => p.CreatedDate)
                     .ThenByDescending(p => p.CreatedDate),

            "created-as" or "created-asc" =>
                query.OrderBy(p => p.CreatedDate),

            "price-asc" =>
                query.OrderBy(p => p.Price),
            "price-desc" =>
                query.OrderByDescending(p => p.Price),

            "name-en-asc" =>
                query.OrderBy(p => p.NameEn),
            "name-en-desc" =>
                query.OrderByDescending(p => p.NameEn),

            // Fallback
            _ => query.OrderByDescending(p => p.CreatedDate)
        };
    }
}