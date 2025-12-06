using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Coupons.Queries.ListCouponsQuery;

public record ListCouponsQuery(
    bool? IsActive = null,
    string? Code = null,
    bool? FreeShipping = null,
    DateTimeOffset? StartDateFrom = null,
    DateTimeOffset? StartDateTo = null,
    DateTimeOffset? EndDateFrom = null,
    DateTimeOffset? EndDateTo = null,
    decimal? FixedAmount = null,
    decimal? Percentage = null
) : IRequest<Result<List<CouponDto>>>;

public class ListCouponsQueryHandler : IRequestHandler<ListCouponsQuery, Result<List<CouponDto>>>
{
    private readonly ApplicationDbContext _db;

    public ListCouponsQueryHandler(ApplicationDbContext db) => _db = db;

    public async Task<Result<List<CouponDto>>> Handle(ListCouponsQuery request, CancellationToken ct)
    {
        var q = _db.Coupons
            .AsNoTracking()
            .ApplyFilters(
                request.IsActive,
                request.Code,
                request.FreeShipping,
                request.StartDateFrom,
                request.StartDateTo,
                request.EndDateFrom,
                request.EndDateTo,
                request.FixedAmount,
                request.Percentage);

        var list = await q
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new CouponDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Code = c.Code,
                FixedAmount = c.FixedAmount,
                Percentage = c.Percentage,
                FreeShipping = c.FreeShipping,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                UsageLimit = c.UsageLimit,
                TimesUsed = c.TimesUsed,
                PerUserLimit = c.PerUserLimit,
                IsActive = c.IsActive
            })
            .ToListAsync(ct);

        return Result<List<CouponDto>>.Success(list);
    }
}