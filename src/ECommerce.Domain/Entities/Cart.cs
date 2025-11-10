using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Cart : BaseAuditableEntity
{
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string? GuestId { get; set; } // used to persist a shopping cart for an unauthenticated (anonymous) visitor
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
