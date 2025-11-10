namespace ECommerce.Application.Features.Products.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
}
