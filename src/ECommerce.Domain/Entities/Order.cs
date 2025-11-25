using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Order : BaseAuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    // Monetary breakdown
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }

    // Shipping/Billing
    public Guid? ShippingAddressId { get; set; }
    public UserAddress? ShippingAddress { get; set; }


    // Shipping method
    public Guid? ShippingMethodId { get; set; }
    public ShippingMethod? ShippingMethod { get; set; }
    public string? TrackingNumber { get; set; }

    // Discounts/Coupons
    public string? CouponCode { get; set; }

    // Statuses
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string? Notes { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
