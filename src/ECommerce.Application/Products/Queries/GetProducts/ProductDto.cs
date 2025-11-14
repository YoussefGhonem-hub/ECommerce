namespace ECommerce.Application.Products.Queries.GetProducts;

public class ProductDto
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double AverageRating { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string CategoryNameEn { get; set; }
    public string CategoryNameAr { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public bool IsInWishlist { get; set; } = false;
    public bool IsInCart { get; set; } = false;
    public string? MainImagePath { get; set; } = "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500";

}
