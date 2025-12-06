using ECommerce.Domain.Entities;

namespace ECommerce.Application.Carts.Queries.GetCartQuery;

public class CheckoutSummaryDto
{
    public CartDto Cart { get; set; } = new CartDto();
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }
    public bool FreeShippingApplied { get; set; }
    public Guid? ShippingMethodId { get; set; }
    public decimal ItemDiscount { get; set; }
    public decimal ShippingDiscount { get; set; }
    public ShippingMethodSummaryDto? SelectedShippingMethod { get; set; }
    public List<CouponDto> Coupons { get; set; } = new();
}

public class CartDto
{
    public Guid Id { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.SubTotal);
}

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameAr { get; set; } = string.Empty;
    public string? MainImagePath { get; set; } =
        "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500";
    public double AverageRating { get; set; }
    public bool IsInWishlist { get; set; }
    public bool IsInCart { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int Quantity { get; set; }
    public decimal SubTotal => Price * Quantity;

    // Line-level user selections (e.g., Color=Black, Size=L)
    public List<CartItemAttributeDto> SelectedAttributes { get; set; } = new();

    // NEW: All available attribute/value options for this product
    public List<CartItemAttributeDto> ProductAttributes { get; set; } = new();

    public List<string> ImageUrls { get; set; } = new();

}

public class CartItemAttributeDto
{
    public Guid AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public Guid? ValueId { get; set; }
    public string? Value { get; set; }
}

public class ShippingMethodSummaryDto
{
    public Guid Id { get; set; }
    public ShippingCostType CostType { get; set; }
    public decimal BaseCost { get; set; }
    public decimal EffectiveCost { get; set; }
    public string? EstimatedTime { get; set; }
    public bool IsDefault { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public bool FreeShippingApplied { get; set; }
    public decimal CalculatedCostWithoutFree { get; set; }
}

