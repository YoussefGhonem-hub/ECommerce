using ECommerce.Application.Orders.Queries.GetMyOrders;
using ECommerce.Application.Products.Queries.GetProducts;
using ECommerce.Domain.Entities;
using Mapster;

namespace ECommerce.Application.Common.Mappings;

public static class MappingConfig
{
    public static void Register()
    {
        // Product -> ProductDto
        TypeAdapterConfig<Product, ProductDto>.NewConfig()
            .Map(dest => dest.CategoryNameEn, src => src.Category != null ? src.Category.NameEn : string.Empty)
            .Map(dest => dest.CategoryNameAr, src => src.Category != null ? src.Category.NameAr : string.Empty)
            .Map(dest => dest.MainImagePath,
                 src => src.Images
                          .Where(i => i.IsMain)
                          .Select(i => i.Path)
                          .FirstOrDefault()
                          ?? "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500");

        TypeAdapterConfig<Order, OrderDto>.NewConfig()
            .Map(d => d.Items, s => s.Items);
        TypeAdapterConfig<OrderItem, OrderItemDto>.NewConfig();
    }
}
