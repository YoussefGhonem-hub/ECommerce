using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class ShippingZone : BaseAuditableEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? City { get; set; }
    public ICollection<ShippingMethod> Methods { get; set; } = new List<ShippingMethod>();
}
