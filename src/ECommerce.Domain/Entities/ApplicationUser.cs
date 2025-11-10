using Microsoft.AspNetCore.Identity;

namespace ECommerce.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public bool IsActive { get; set; } = true;
}
