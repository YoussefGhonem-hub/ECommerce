namespace ECommerce.Application.Features.Products.Queries.GetProducts;

public class ProductDto
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
}
