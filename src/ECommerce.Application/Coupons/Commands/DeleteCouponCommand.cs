using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Coupons.Commands;

public record DeleteCouponCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteCouponCommandHandler : IRequestHandler<DeleteCouponCommand, Result<bool>>
{
    private readonly ApplicationDbContext _db;

    public DeleteCouponCommandHandler(ApplicationDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteCouponCommand request, CancellationToken ct)
    {
        var entity = await _db.Coupons
            .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, ct);

        if (entity is null)
            return Result<bool>.Failure("Coupon not found.");

        // Optional integrity cleanup: remove usages of this coupon
        // var usages = await _db.CouponUsages.Where(u => u.CouponId == request.Id).ToListAsync(ct);
        // if (usages.Count > 0) _db.CouponUsages.RemoveRange(usages);

        entity.MarkAsDeleted(Guid.Empty); // replace with current user id if available

        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}