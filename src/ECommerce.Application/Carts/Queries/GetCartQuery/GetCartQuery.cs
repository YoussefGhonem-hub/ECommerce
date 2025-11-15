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
                    .ThenInclude(p => p.ProductSettings)
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

        // Gather product IDs
        var productIds = cartEntity.Items.Select(i => i.ProductId).Distinct().ToList();
        var now = DateTimeOffset.UtcNow;

        // Load active settings affecting these products
        var activeSettings = await _context.ProductSettings
            .Include(ps => ps.Products)
            .Where(ps => ps.IsActive
                         && ps.StartDate <= now
                         && ps.EndDate >= now
                         && (ps.AppliesToAllProducts || ps.Products.Any(p => productIds.Contains(p.Id))))
            .ToListAsync(cancellationToken);

        // Compute itemDiscount
        decimal itemDiscount = 0m;

        foreach (var item in cartEntity.Items)
        {
            if (item.Product is null) continue;

            var settingsForProduct = activeSettings.Where(ps =>
                ps.AppliesToAllProducts || ps.Products.Any(p => p.Id == item.ProductId));

            if (!settingsForProduct.Any()) continue;

            decimal productDiscount = 0m;

            foreach (var setting in settingsForProduct)
            {
                productDiscount += ComputeDiscount(setting, item.Product.Price, item.Quantity);
            }

            // Cap discount at line subtotal
            var lineSubtotal = item.Product.Price * item.Quantity;
            if (productDiscount > lineSubtotal)
                productDiscount = lineSubtotal;

            itemDiscount += productDiscount;
        }

        // Determine product owner (seller) from cart items:
        // Prefer Product.UserId; if null/empty, fallback to Product.CreatedBy from BaseAuditableEntity.
        var sellerIds = cartEntity.Items
            .Where(i => i.Product != null)
            .Select(i =>
            {
                var p = i.Product!;
                if (p.UserId.HasValue && p.UserId.Value != Guid.Empty)
                    return p.UserId.Value;
                return p.CreatedBy != Guid.Empty ? p.CreatedBy : Guid.Empty;
            })
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        Guid? shippingUserId = sellerIds.FirstOrDefault(); // if multiple sellers, pick the first; extend to multi-seller if needed

        // Shipping resolution (use product owner for shipping methods)
        var methodSummary = await ResolveShippingAsync(shippingUserId, subTotal, cancellationToken);
        var shippingDiscount = (methodSummary?.FreeShippingApplied ?? false)
            ? (methodSummary?.CalculatedCostWithoutFree ?? 0m)
            : 0m;

        discount = itemDiscount + shippingDiscount;

        // Retrieve valid coupons for display
        var coupons = await GetValidCouponsForCurrentUserAsync(cartEntity.Items.Select(i => i.Product).FirstOrDefault()!, userId, cancellationToken);

        var summary = new CheckoutSummaryDto
        {
            Cart = cartDto,
            SubTotal = subTotal,
            ItemDiscount = itemDiscount,
            ShippingDiscount = shippingDiscount,
            DiscountTotal = discount,
            ShippingTotal = methodSummary?.EffectiveCost ?? 0m,
            Total = subTotal - discount + (methodSummary?.EffectiveCost ?? 0m),
            ShippingMethodId = methodSummary?.Id,
            FreeShippingApplied = methodSummary?.FreeShippingApplied ?? false,
            SelectedShippingMethod = methodSummary,
            Coupons = coupons
        };

        return Result<CheckoutSummaryDto>.Success(summary);
    }

    private async Task<List<CouponDto>> GetValidCouponsForCurrentUserAsync(Product Product, Guid? userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Base valid coupons: active, within date range, not exceeding global usage
        var baseCoupons = await _context.Coupons
            .AsNoTracking()
            .Where(c => c.IsActive
                        && c.StartDate <= now
                        && c.UserId == Product.UserId
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

    // CHANGED: returns full summary of the default method + effective cost
    private async Task<ShippingMethodSummaryDto?> ResolveShippingAsync(Guid? userId, decimal subTotal, CancellationToken ct)
    {

        var method = await _context.ShippingMethods.FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault);

        if (method is null)
            return null;

        // Compute effective cost honoring free shipping threshold
        var freeApplied = method.FreeShippingThreshold.HasValue && subTotal >= method.FreeShippingThreshold.Value;

        decimal effectiveCost;
        if (freeApplied)
        {
            effectiveCost = 0m;
        }
        else
        {
            switch (method.CostType)
            {
                case ShippingCostType.Flat:
                    effectiveCost = method.Cost;
                    break;
                case ShippingCostType.ByTotal:
                    effectiveCost = Math.Round(subTotal * (method.Cost / 100m), 2, MidpointRounding.AwayFromZero);
                    break;
                case ShippingCostType.ByWeight:
                    effectiveCost = method.Cost; // weights not modeled
                    break;
                default:
                    effectiveCost = method.Cost;
                    break;
            }
        }

        return new ShippingMethodSummaryDto
        {
            Id = method.Id,
            CostType = method.CostType,
            BaseCost = method.Cost,
            EffectiveCost = effectiveCost,
            EstimatedTime = method.EstimatedTime,
            IsDefault = method.IsDefault,
            FreeShippingThreshold = method.FreeShippingThreshold,
            FreeShippingApplied = freeApplied
        };
    }

    // Local function
    private decimal ComputeDiscount(ProductSetting setting, decimal unitPrice, int qty)
    {
        var line = unitPrice * qty;
        return setting.Kind switch
        {
            DiscountKind.Percentage => Math.Round(line * (setting.Value / 100m), 2, MidpointRounding.AwayFromZero),
            DiscountKind.FixedAmount => Math.Round(setting.Value * qty, 2, MidpointRounding.AwayFromZero),
            _ => 0m
        };
    }
}