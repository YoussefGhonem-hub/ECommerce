using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

// Captures the attribute selection made for an OrderItem.
// Keeps both FKs and snapshot strings for historical accuracy.
public class OrderItemAttribute : BaseAuditableEntity
{
    public Guid OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    // Reference to the attribute and its chosen value (when applicable)
    public Guid ProductAttributeId { get; set; }
    public ProductAttribute? ProductAttribute { get; set; }
    public Guid? ProductAttributeValueId { get; set; }
    public ProductAttributeValue? ProductAttributeValue { get; set; }

    // Snapshots at the time of ordering (safe if catalog changes later)
    public string AttributeName { get; set; } = string.Empty;
    public string? Value { get; set; }
}