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
                .ThenInclude(i => i.Attributes)
            .FirstOrDefaultAsync(cancellationToken);

        if (cartEntity is null)
        {
            return Result<CheckoutSummaryDto>.Success(new CheckoutSummaryDto
            {
                Cart = new CartDto(),
                SubTotal = 0,
                ShippingTotal = 0,
                Total = 0,
                Coupons = new List<CouponDto>()
            });
        }

        // Map entity graph to CartDto (Mapster mappings must be registered)
        var cartDto = cartEntity.Adapt<CartDto>();

        var subTotal = cartDto.Total;
        var discount = 0m; // extend with coupon application if needed

        // Shipping resolution
        var (methodId, shippingCost, freeApplied) =
            await ResolveShippingAsync(userId, subTotal, cancellationToken);

        // Retrieve valid coupons for display
        var coupons = await GetValidCouponsForCurrentUserAsync(userId, cancellationToken);

        var summary = new CheckoutSummaryDto
        {
            Cart = cartDto,
            SubTotal = subTotal,
            DiscountTotal = discount,
            ShippingTotal = shippingCost,
            Total = subTotal - discount + shippingCost,
            ShippingMethodId = methodId,
            FreeShippingApplied = freeApplied,
            Coupons = coupons
        };

        return Result<CheckoutSummaryDto>.Success(summary);
    }

    private async Task<List<CouponDto>> GetValidCouponsForCurrentUserAsync(Guid? userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Base valid coupons: active, within date range, not exceeding global usage
        var baseCoupons = await _context.Coupons
            .AsNoTracking()
            .Where(c => c.IsActive
                        && c.StartDate <= now
                        && c.EndDate >= now
                        && (!c.UsageLimit.HasValue || c.TimesUsed < c.UsageLimit.Value))
            .Select(c => new CouponDto
            {
                Id = c.Id,
                Code = c.Code,
                FixedAmount = c.FixedAmount,
                Percentage = c.Percentage,
                FreeShipping = c.FreeShipping,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsActive = c.IsActive,
                UsageLimit = c.UsageLimit,
                TimesUsed = c.TimesUsed,
                PerUserLimit = c.PerUserLimit,
                RemainingPerUserUses = null // set below if applicable
            })
            .ToListAsync(ct);

        if (userId is null)
        {
            // No per-user filtering for guests
            return baseCoupons;
        }

        var userKey = userId.Value.ToString();

        // Get per-user usage counts for all coupons in one query
        var couponIds = baseCoupons.Select(b => b.Id).ToList();
        var perUserUsage = await _context.CouponUsages
            .AsNoTracking()
            .Where(u => u.UserId == userKey && couponIds.Contains(u.CouponId))
            .GroupBy(u => u.CouponId)
            .Select(g => new { CouponId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var perUserDict = perUserUsage.ToDictionary(x => x.CouponId, x => x.Count);

        // Filter out coupons that exceed per-user limit and compute remaining per-user uses
        var result = new List<CouponDto>(baseCoupons.Count);
        foreach (var c in baseCoupons)
        {
            if (c.PerUserLimit.HasValue)
            {
                var used = perUserDict.TryGetValue(c.Id, out var cnt) ? cnt : 0;
                var remaining = Math.Max(c.PerUserLimit.Value - used, 0);
                c.RemainingPerUserUses = remaining;

                if (remaining <= 0)
                    continue; // user exceeded their personal limit; hide this coupon
            }

            result.Add(c);
        }

        return result;
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
            .FirstOrDefaultAsync(z =>
                z.CountryId == address.CountryId &&
                (z.CityId == null || z.CityId == address.CityId), ct);

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
                cost = method.Cost;
                break;
            default:
                cost = method.Cost;
                break;
        }

        return (method.Id, cost, false);
    }
}