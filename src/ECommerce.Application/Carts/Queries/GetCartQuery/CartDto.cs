namespace ECommerce.Application.Carts.Queries.GetCartQuery;

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

    public List<CartItemAttributeDto> SelectedAttributes { get; set; } = new();
}

public class CartItemAttributeDto
{
    public Guid AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public Guid? ValueId { get; set; }
    public string? Value { get; set; }
}
