using ECommerce.Domain.Entities;

namespace ECommerce.Application.Coupons.Queries.ListCouponsQuery;

internal static class CouponQueryExtensions
{
    public static IQueryable<Coupon> ApplyFilters(
        this IQueryable<Coupon> query,
        bool? isActive,
        string? code,
        bool? freeShipping,
        DateTimeOffset? startDateFrom,
        DateTimeOffset? startDateTo,
        DateTimeOffset? endDateFrom,
        DateTimeOffset? endDateTo,
        decimal? fixedAmount,
        decimal? percentage)
    {
        // Always exclude soft-deleted
        query = query.Where(c => !c.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(code))
        {
            var normalized = code.Trim();
            query = query.Where(c => c.Code.Contains(normalized));
        }

        if (freeShipping.HasValue)
            query = query.Where(c => c.FreeShipping == freeShipping.Value);

        // StartDate range
        if (startDateFrom.HasValue)
            query = query.Where(c => c.StartDate >= startDateFrom.Value);
        if (startDateTo.HasValue)
            query = query.Where(c => c.StartDate <= startDateTo.Value);

        // EndDate range
        if (endDateFrom.HasValue)
            query = query.Where(c => c.EndDate >= endDateFrom.Value);
        if (endDateTo.HasValue)
            query = query.Where(c => c.EndDate <= endDateTo.Value);

        // Exact amount filters (if you need ranges, change condition accordingly)
        if (fixedAmount.HasValue)
            query = query.Where(c => c.FixedAmount.HasValue && c.FixedAmount.Value == fixedAmount.Value);

        if (percentage.HasValue)
            query = query.Where(c => c.Percentage.HasValue && c.Percentage.Value == percentage.Value);

        return query;
    }
}