using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Coupons.Queries.GetCouponById;

public record GetCouponByIdQuery(Guid Id) : IRequest<Result<CouponDto>>;

public class GetCouponByIdQueryHandler : IRequestHandler<GetCouponByIdQuery, Result<CouponDto>>
{
    private readonly ApplicationDbContext _db;

    public GetCouponByIdQueryHandler(ApplicationDbContext db) => _db = db;

    public async Task<Result<CouponDto>> Handle(GetCouponByIdQuery request, CancellationToken ct)
    {
        var c = await _db.Coupons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (c is null)
            return Result<CouponDto>.Failure("Coupon not found.");

        return Result<CouponDto>.Success(new CouponDto
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
        });
    }
}