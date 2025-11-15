using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class ShippingZone : BaseAuditableEntity
{
    public Guid CountryId { get; set; }
    public Country? Country { get; set; }

    public Guid? CityId { get; set; }
    public City? City { get; set; }

    // Many-to-many: a zone can be attached to many shipping methods, and vice versa
    public ICollection<ShippingMethod> Methods { get; set; } = new List<ShippingMethod>();
}
