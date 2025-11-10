using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class ProductImage : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    // Relative path under wwwroot, e.g., "productimages/xxxx.jpg"
    public string Path { get; set; } = string.Empty;

    // Only one per product should be true
    public bool IsMain { get; set; } = false;

    public int SortOrder { get; set; } = 0;
}