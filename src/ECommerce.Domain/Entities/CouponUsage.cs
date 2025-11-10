using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class CouponUsage : BaseAuditableEntity
{
    public Guid CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public string UserId { get; set; } = string.Empty;
}
