using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class ShippingMethod : BaseAuditableEntity
{
    public Guid ShippingZoneId { get; set; }
    public ShippingZone? ShippingZone { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string? EstimatedTime { get; set; }
    public string CostType { get; set; } = "Flat"; // Flat, ByWeight, ByTotal
}
