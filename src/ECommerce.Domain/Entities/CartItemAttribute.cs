using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

// Captures selected product attributes for a cart line.
public class CartItemAttribute : BaseAuditableEntity
{
    public Guid CartItemId { get; set; }
    public CartItem? CartItem { get; set; }

    public Guid ProductAttributeId { get; set; }
    public ProductAttribute? ProductAttribute { get; set; }
    public Guid? ProductAttributeValueId { get; set; }
    public ProductAttributeValue? ProductAttributeValue { get; set; }

    // Snapshots for display stability if catalog changes
    public string AttributeName { get; set; } = string.Empty;
    public string? Value { get; set; }
}