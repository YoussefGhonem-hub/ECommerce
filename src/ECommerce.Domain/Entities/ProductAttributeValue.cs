using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

//	Defines allowed values for an attribute, e.g., for “Color” → “Red”, “Blue”; for “Size” → “M”, “L”.
public class ProductAttributeValue : BaseAuditableEntity
{
    public Guid ProductAttributeId { get; set; }
    public ProductAttribute? ProductAttribute { get; set; }
    public string Value { get; set; } = string.Empty;
}
