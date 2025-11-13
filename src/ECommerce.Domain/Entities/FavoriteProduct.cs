using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class FavoriteProduct : BaseAuditableEntity
{
    public Guid? UserId { get; set; }
    public string? GuestId { get; set; }
    public ApplicationUser? User { get; set; }
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
}
