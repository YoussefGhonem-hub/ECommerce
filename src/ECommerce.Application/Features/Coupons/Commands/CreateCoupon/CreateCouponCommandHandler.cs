using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Coupons.Commands.CreateCoupon;

public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateCouponCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Coupons.AnyAsync(c => c.Code == request.Code, cancellationToken);
        if (exists) return Result<Guid>.Failure("Coupon code already exists");

        var coupon = new Coupon
        {
            Code = request.Code,
            FixedAmount = request.FixedAmount,
            Percentage = request.Percentage,
            FreeShipping = request.FreeShipping,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UsageLimit = request.UsageLimit,
            PerUserLimit = request.PerUserLimit,
            IsActive = true
        };

        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(coupon.Id);
    }
}
