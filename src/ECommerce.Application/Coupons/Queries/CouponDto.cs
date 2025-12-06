namespace ECommerce.Application.Coupons.Queries;
public class CouponDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal? FixedAmount { get; set; }
    public decimal? Percentage { get; set; }
    public bool FreeShipping { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; }
    public int? PerUserLimit { get; set; }
    public bool IsActive { get; set; }
}