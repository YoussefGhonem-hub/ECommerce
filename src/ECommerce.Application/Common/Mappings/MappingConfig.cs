using ECommerce.Application.Carts.Queries.GetCartQuery;
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
        // Product list item mapping
        TypeAdapterConfig<Product, ProductDto>.NewConfig()
            .Map(d => d.CategoryNameEn, s => s.Category != null ? s.Category.NameEn : string.Empty)
            .Map(d => d.CategoryNameAr, s => s.Category != null ? s.Category.NameAr : string.Empty)
            .Map(d => d.MainImagePath,
                 s => s.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
                      ?? "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500")
            .Map(d => d.IsInWishlist,
                 s => s.FavoriteProducts.Any(fp => fp.UserId == CurrentUser.Id)
                   || s.FavoriteProducts.Any(fp => fp.GuestId == CurrentUser.GuestId))
            .Map(d => d.Description, s => s.DescriptionEn ?? s.DescriptionAr);

        // Product details mapping
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
            .Map(d => d.IsInWishlist,
                 s => s.FavoriteProducts.Any(fp => fp.UserId == CurrentUser.Id)
                   || s.FavoriteProducts.Any(fp => fp.GuestId == CurrentUser.GuestId));

        // Cart -> CartDto
        TypeAdapterConfig<Cart, CartDto>.NewConfig()
            .Map(d => d.Id, s => s.Id)
            .Map(d => d.Items, s => s.Items);

        // CartItem -> CartItemDto (reads Product info)
        TypeAdapterConfig<CartItem, CartItemDto>.NewConfig()
            .Map(d => d.Id, s => s.Id)
            .Map(d => d.ProductId, s => s.ProductId)
            .Map(d => d.Quantity, s => s.Quantity)
            .Map(d => d.Price, s => s.Product != null ? s.Product.Price : 0m)
            .Map(d => d.StockQuantity, s => s.Product != null ? s.Product.StockQuantity : 0)
            .Map(d => d.ProductName, s => s.Product != null ? (s.Product.NameEn ?? s.Product.NameAr ?? string.Empty) : string.Empty)
            .Map(d => d.Brand, s => s.Product != null ? s.Product.Brand : null)
            .Map(d => d.CategoryNameEn, s => s.Product != null && s.Product.Category != null ? s.Product.Category.NameEn : string.Empty)
            .Map(d => d.CategoryNameAr, s => s.Product != null && s.Product.Category != null ? s.Product.Category.NameAr : string.Empty)
            .Map(d => d.MainImagePath,
                 s => s.Product != null
                    ? (s.Product.Images.Where(img => img.IsMain).Select(img => img.Path).FirstOrDefault()
                       ?? "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500")
                    : "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500")
            .Map(d => d.AverageRating,
                 s => s.Product != null
                    ? (s.Product.ProductReviews.Where(r => r.IsApproved).Select(r => (double?)r.Rating).Average() ?? 0d)
                    : 0d)
            .Map(d => d.IsInWishlist,
                 s => s.Product != null &&
                      (s.Product.FavoriteProducts.Any(fp => CurrentUser.Id != null && fp.UserId == CurrentUser.Id) ||
                       s.Product.FavoriteProducts.Any(fp => CurrentUser.GuestId != null && fp.GuestId == CurrentUser.GuestId)))
            .Map(d => d.IsInCart, s => true)
            .Map(d => d.SelectedAttributes, s => s.Attributes);

        // CartItemAttribute -> CartItemAttributeDto
        TypeAdapterConfig<CartItemAttribute, CartItemAttributeDto>.NewConfig()
            .Map(d => d.AttributeId, s => s.ProductAttributeId)
            .Map(d => d.ValueId, s => s.ProductAttributeValueId)
            .Map(d => d.AttributeName, s => s.AttributeName)
            .Map(d => d.Value, s => s.Value);

        // Orders mapping (existing)
        TypeAdapterConfig<Order, OrderDto>.NewConfig()
            .Map(d => d.Items, s => s.Items);
        TypeAdapterConfig<OrderItem, OrderItemDto>.NewConfig();
    }
}