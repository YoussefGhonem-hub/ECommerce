using ECommerce.Application.Orders.Queries.GetMyOrders;
using ECommerce.Domain.Entities;
using Mapster;

namespace ECommerce.Application.Common.Mappings;

public static class MappingConfig
{
    public static void Register()
    {
        //TypeAdapterConfig<Product, ProductDto>.NewConfig();
        //TypeAdapterConfig<Category, CategoryDto>.NewConfig();
        TypeAdapterConfig<Order, OrderDto>.NewConfig()
            .Map(d => d.Items, s => s.Items);
        TypeAdapterConfig<OrderItem, OrderItemDto>.NewConfig();
        //TypeAdapterConfig<DashboardStats, DashboardStatsDto>.NewConfig();
    }
}
