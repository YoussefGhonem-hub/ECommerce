using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Coupons.Commands;

public record CreateCouponCommand(
    Guid? UserId,
    string Code,
    decimal? FixedAmount,
    decimal? Percentage,
    bool FreeShipping,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    int? UsageLimit,
    int? PerUserLimit,
    bool IsActive
) : IRequest<Result<Guid>>;

public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _db;

    public CreateCouponCommandHandler(ApplicationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateCouponCommand request, CancellationToken ct)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(request.Code))
            return Result<Guid>.Failure("Coupon code is required.");
        if (await _db.Coupons.AnyAsync(c => c.Code == request.Code && !c.IsDeleted, ct))
            return Result<Guid>.Failure("Coupon code already exists.");
        if (request.StartDate >= request.EndDate)
            return Result<Guid>.Failure("StartDate must be before EndDate.");
        if (request.FixedAmount.HasValue && request.FixedAmount < 0)
            return Result<Guid>.Failure("FixedAmount cannot be negative.");
        if (request.Percentage.HasValue && (request.Percentage < 0 || request.Percentage > 100))
            return Result<Guid>.Failure("Percentage must be between 0 and 100.");

        var coupon = new Coupon
        {
            UserId = request.UserId,
            Code = request.Code.Trim(),
            FixedAmount = request.FixedAmount,
            Percentage = request.Percentage,
            FreeShipping = request.FreeShipping,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UsageLimit = request.UsageLimit,
            TimesUsed = 0,
            PerUserLimit = request.PerUserLimit,
            IsActive = request.IsActive
        };

        coupon.MarkAsCreated(Guid.Empty); // replace with current user id if available

        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(coupon.Id);
    }
}