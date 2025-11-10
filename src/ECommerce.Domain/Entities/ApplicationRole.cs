using Microsoft.AspNetCore.Identity;

namespace ECommerce.Domain.Entities;
public class ApplicationRole : IdentityRole<Guid>
{
    public string? DisplayName { get; set; }
}
