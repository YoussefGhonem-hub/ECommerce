using Microsoft.AspNetCore.Identity;

namespace ECommerce.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public bool IsActive { get; set; } = true;

    // NEW: owned object for social links
    public UserSocialProfiles SocialProfiles { get; set; } = new();
}
