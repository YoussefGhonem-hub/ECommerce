using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class OrderItem : BaseAuditableEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal UnitPrice { get; set; } //  is the per‑item snapshot price stored on the order line at the moment the order is created.
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal LineTotal => (UnitPrice - Discount + Tax) * Quantity;
}
