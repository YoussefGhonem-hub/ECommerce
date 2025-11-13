using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class OrderItem : BaseAuditableEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal UnitPrice { get; set; } // per-item snapshot price when order is created
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal LineTotal => (UnitPrice - Discount + Tax) * Quantity;

    // Selected attributes for this line (e.g., Size=M, Color=Red)
    public ICollection<OrderItemAttribute> Attributes { get; set; } = new List<OrderItemAttribute>();
}
