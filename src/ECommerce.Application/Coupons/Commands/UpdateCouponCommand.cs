using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Coupons.Commands;

public record UpdateCouponCommand(
    Guid Id,
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

public class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _db;

    public UpdateCouponCommandHandler(ApplicationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(UpdateCouponCommand request, CancellationToken ct)
    {
        var entity = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, ct);
        if (entity is null)
            return Result<Guid>.Failure("Coupon not found.");

        if (string.IsNullOrWhiteSpace(request.Code))
            return Result<Guid>.Failure("Coupon code is required.");
        if (await _db.Coupons.AnyAsync(c => c.Code == request.Code && c.Id != request.Id && !c.IsDeleted, ct))
            return Result<Guid>.Failure("Another coupon with the same code already exists.");
        if (request.StartDate >= request.EndDate)
            return Result<Guid>.Failure("StartDate must be before EndDate.");
        if (request.FixedAmount.HasValue && request.FixedAmount < 0)
            return Result<Guid>.Failure("FixedAmount cannot be negative.");
        if (request.Percentage.HasValue && (request.Percentage < 0 || request.Percentage > 100))
            return Result<Guid>.Failure("Percentage must be between 0 and 100.");

        entity.UserId = request.UserId;
        entity.Code = request.Code.Trim();
        entity.FixedAmount = request.FixedAmount;
        entity.Percentage = request.Percentage;
        entity.FreeShipping = request.FreeShipping;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.UsageLimit = request.UsageLimit;
        entity.PerUserLimit = request.PerUserLimit;
        entity.IsActive = request.IsActive;

        entity.MarkAsModified(Guid.Empty); // replace with current user id if available

        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(entity.Id);
    }
}