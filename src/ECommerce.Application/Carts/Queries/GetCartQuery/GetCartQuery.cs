using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Carts.Queries.GetCartQuery;

public record GetCartQuery() : IRequest<Result<CheckoutSummaryDto>>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<CheckoutSummaryDto>>
{
    private readonly ApplicationDbContext _context;

    public GetCartQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CheckoutSummaryDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id;
        var guestId = CurrentUser.GuestId;

        // Load full cart graph with all needed navigations
        var cartEntity = await _context.Carts
            .AsNoTracking()
            .Where(c =>
                (userId != null && c.UserId == userId) ||
                (guestId != null && c.GuestId == guestId))
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.ProductReviews)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.FavoriteProducts)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Include(c => c.Items)
                    .ThenInclude(p => p.Attributes)
            .FirstOrDefaultAsync(cancellationToken);

        if (cartEntity is null)
        {
            return Result<CheckoutSummaryDto>.Success(new CheckoutSummaryDto
            {
                Cart = new CartDto(),
                SubTotal = 0,
                ShippingTotal = 0,
                Total = 0
            });
        }

        // Map entity graph to CartDto (requires Mapster mappings for Cart, CartItem, CartItemAttribute)
        var cartDto = cartEntity.Adapt<CartDto>();

        var subTotal = cartDto.Total;
        var discount = 0m; // extend with coupon logic later

        var (methodId, shippingCost, freeApplied) =
            await ResolveShippingAsync(userId, subTotal, cancellationToken);

        var summary = new CheckoutSummaryDto
        {
            Cart = cartDto,
            SubTotal = subTotal,
            DiscountTotal = discount,
            ShippingTotal = shippingCost,
            Total = subTotal - discount + shippingCost,
            ShippingMethodId = methodId,
            FreeShippingApplied = freeApplied
        };

        return Result<CheckoutSummaryDto>.Success(summary);
    }

    private async Task<(Guid? MethodId, decimal ShippingCost, bool FreeApplied)>
        ResolveShippingAsync(Guid? userId, decimal subTotal, CancellationToken ct)
    {
        if (userId is null)
            return (null, 0m, false);

        var address = await _context.UserAddresses
            .OrderByDescending(a => a.IsDefault)
            .FirstOrDefaultAsync(a => a.UserId == userId, ct);

        if (address is null)
            return (null, 0m, false);

        var zone = await _context.ShippingZones
            .Include(z => z.Methods)
            .FirstOrDefaultAsync(z => z.Methods.FirstOrDefault(x => x.IsDefault) != null, ct);

        var method = zone?.Methods
            .OrderByDescending(m => m.IsDefault)
            .FirstOrDefault();

        if (method is null)
            return (null, 0m, false);

        if (method.FreeShippingThreshold.HasValue &&
            subTotal >= method.FreeShippingThreshold.Value)
            return (method.Id, 0m, true);

        decimal cost = method.Cost;
        switch (method.CostType)
        {
            case ShippingCostType.Flat:
                cost = method.Cost;
                break;
            case ShippingCostType.ByTotal:
                cost = Math.Round(subTotal * (method.Cost / 100m), 2, MidpointRounding.AwayFromZero);
                break;
            case ShippingCostType.ByWeight:
                cost = method.Cost; // fallback (no weights modeled)
                break;
            default:
                cost = method.Cost;
                break;
        }

        return (method.Id, cost, false);
    }
}