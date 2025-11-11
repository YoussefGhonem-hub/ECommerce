using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal Price { get; set; }
    public decimal? Cost { get; set; }
    public int StockQuantity { set; get; }
    public bool AllowBackorder { get; set; }
    public bool IsInWishlist { get; set; } = false;
    public bool IsInCart { get; set; } = false;
    public string? Brand { get; set; }
    public string? Color { get; set; }
    public double AverageRating { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public string? MainImagePath => Images.FirstOrDefault(i => i.IsMain)?.Path;
}
