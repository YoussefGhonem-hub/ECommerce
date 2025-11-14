using ECommerce.Application.Orders.Queries.GetMyOrders;
using ECommerce.Application.Products.Queries.GetProductById;
using ECommerce.Application.Products.Queries.GetProducts;
using ECommerce.Domain.Entities;
using ECommerce.Shared.CurrentUser;
using Mapster;

namespace ECommerce.Application.Common.Mappings;

public static class MappingConfig
{
    public static void Register()
    {
        // Product list item mapping (enhanced)
        TypeAdapterConfig<Product, ProductDto>.NewConfig()
            .Map(d => d.CategoryNameEn, s => s.Category != null ? s.Category.NameEn : string.Empty)
            .Map(d => d.CategoryNameAr, s => s.Category != null ? s.Category.NameAr : string.Empty)
            .Map(d => d.MainImagePath,
                 s => s.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
                      ?? "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500")
            // Compute IsInWishlist using parameters passed via ProjectToType(..., new { UserId, GuestId })
            .Map(d => d.IsInWishlist,
                 s => s.FavoriteProducts.Any(fp => fp.UserId == CurrentUser.Id)
                   || s.FavoriteProducts.Any(fp => fp.GuestId == CurrentUser.GuestId))
            // Optional: unify description fallback
            .Map(d => d.Description, s => s.DescriptionEn ?? s.DescriptionAr);

        // Product details mapping (enhanced)
        TypeAdapterConfig<Product, ProductDetailsDto>.NewConfig()
            .Map(d => d.CategoryNameEn, s => s.Category != null ? s.Category.NameEn : string.Empty)
            .Map(d => d.CategoryNameAr, s => s.Category != null ? s.Category.NameAr : string.Empty)
            .Map(d => d.MainImagePath,
                 s => s.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
                      ?? "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500")
            .Map(d => d.Images,
                 s => s.Images
                       .OrderBy(i => i.SortOrder)
                       .Select(i => new ProductImageDto
                       {
                           Path = i.Path,
                           IsMain = i.IsMain,
                           SortOrder = i.SortOrder
                       }))
            .Map(d => d.Reviews,
                 s => s.ProductReviews
                       .Where(r => r.IsApproved)
                       .OrderByDescending(r => r.CreatedDate)
                       .Select(r => new ProductReviewDto
                       {
                           Id = r.Id,
                           Rating = r.Rating,
                           Comment = r.Comment,
                           CreatedDate = r.CreatedDate,
                           UserFullName = r.User.FullName
                       }))
            .Map(d => d.AverageRating,
                 s => (double?)s.ProductReviews
                                .Where(r => r.IsApproved)
                                .Select(r => (double?)r.Rating)
                                .Average() ?? 0d)
            // IsInWishlist via parameters
            .Map(d => d.IsInWishlist,
                 s => s.FavoriteProducts.Any(fp => fp.UserId == CurrentUser.Id)
                   || s.FavoriteProducts.Any(fp => fp.GuestId == CurrentUser.GuestId));

        // Orders mapping (existing)
        TypeAdapterConfig<Order, OrderDto>.NewConfig()
            .Map(d => d.Items, s => s.Items);
        TypeAdapterConfig<OrderItem, OrderItemDto>.NewConfig();
    }
}
