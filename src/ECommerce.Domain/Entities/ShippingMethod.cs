using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class ShippingMethod : BaseAuditableEntity
{
    // Optional: target specific user for offers
    public ApplicationUser? User { get; set; }
    public Guid? UserId { get; set; }

    public decimal Cost { get; set; }
    public ShippingCostType CostType { get; set; }
    public string? EstimatedTime { get; set; }
    public bool IsDefault { get; set; } = false;
    public decimal? FreeShippingThreshold { get; set; } // subtotal >= threshold => free

    // Many-to-many: this method applies to many zones (e.g., free shipping for a specific city)
    public ICollection<ShippingZone> Zones { get; set; } = new List<ShippingZone>();
}

public enum ShippingCostType
{
    Flat,
    ByWeight,
    ByTotal
}
