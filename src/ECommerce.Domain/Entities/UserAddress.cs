using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class UserAddress : BaseAuditableEntity
{
    public ApplicationUser User { get; set; }
    public Guid? UserId { get; set; }
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public bool IsDefault { get; set; }
}
