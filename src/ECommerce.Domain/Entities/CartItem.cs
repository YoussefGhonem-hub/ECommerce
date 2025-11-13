using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class CartItem : BaseAuditableEntity
{
    public Guid CartId { get; set; }
    public Cart? Cart { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }

    // Selected attributes (e.g., Size=M, Color=Red)
    public ICollection<CartItemAttribute> Attributes { get; set; } = new List<CartItemAttribute>();
}
