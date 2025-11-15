namespace ECommerce.Application.Carts.Queries.GetCartQuery;

public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;

    public decimal? FixedAmount { get; set; }
    public decimal? Percentage { get; set; }
    public bool FreeShipping { get; set; }

    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public bool IsActive { get; set; }

    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; }
    public int? PerUserLimit { get; set; }

    public int? RemainingGlobalUses =>
        UsageLimit.HasValue ? Math.Max(UsageLimit.Value - TimesUsed, 0) : null;

    public int? RemainingPerUserUses { get; set; } // computed for current user; null if no per-user limit
}