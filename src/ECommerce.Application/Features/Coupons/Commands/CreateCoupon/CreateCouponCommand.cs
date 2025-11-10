using MediatR;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Coupons.Commands.CreateCoupon;

public record CreateCouponCommand(string Code, decimal? FixedAmount, decimal? Percentage, bool FreeShipping, DateTime StartDate, DateTime EndDate, int? UsageLimit, int? PerUserLimit)
    : IRequest<Result<Guid>>;
