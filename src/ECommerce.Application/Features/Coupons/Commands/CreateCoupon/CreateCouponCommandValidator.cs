using FluentValidation;

namespace ECommerce.Application.Features.Coupons.Commands.CreateCoupon;

public class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate);
    }
}
