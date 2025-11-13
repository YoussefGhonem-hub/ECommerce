using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

// •	Defines an attribute type, e.g., “Color”, “Size”.
public class ProductAttribute : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
}
