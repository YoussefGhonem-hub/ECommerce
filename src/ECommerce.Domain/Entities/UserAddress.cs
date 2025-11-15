using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class UserAddress : BaseAuditableEntity
{
    public ApplicationUser? User { get; set; }
    public Guid? UserId { get; set; }
    public string? FullName { get; set; }
    public Guid CountryId { get; set; }
    public Country Country { get; set; } = new Country();
    public Guid CityId { get; set; }
    public City City { get; set; } = new City();
    public string Street { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string? HouseNo { get; set; }
    public bool IsDefault { get; set; } = false;
}
