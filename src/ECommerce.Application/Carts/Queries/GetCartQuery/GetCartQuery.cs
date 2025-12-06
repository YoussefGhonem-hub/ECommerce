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

        var cartEntity = await LoadCartAsync(userId, guestId, cancellationToken);
        if (cartEntity is null)
            return Result<CheckoutSummaryDto>.Success(CreateEmptySummary());

        // Map and enrich cart dto
        var cartDto = await BuildCartDtoAsync(cartEntity, cancellationToken);

        var subTotal = cartDto.Total;
        var productIds = cartEntity.Items.Select(i => i.ProductId).Distinct().ToList();
        var now = DateTimeOffset.UtcNow;

        var activeSettings = await LoadActiveProductSettingsAsync(productIds, now, cancellationToken);
        var itemDiscount = ComputeItemDiscount(cartEntity, activeSettings);

        var shippingUserId = GetShippingUserId(cartEntity);
        var methodSummary = await ResolveShippingAsync(shippingUserId, subTotal, cancellationToken);
        var shippingDiscount = (methodSummary?.FreeShippingApplied ?? false)
            ? (methodSummary?.CalculatedCostWithoutFree ?? 0m)
            : 0m;

        var discountTotal = itemDiscount + shippingDiscount;

        var coupons = await GetValidCouponsForCurrentUserAsync(
            cartEntity.Items.Select(i => i.Product).FirstOrDefault()!,
            userId,
            cancellationToken);

        var summary = BuildSummary(
            cartDto,
            subTotal,
            itemDiscount,
            shippingDiscount,
            discountTotal,
            methodSummary,
            coupons);

        return Result<CheckoutSummaryDto>.Success(summary);
    }

    // Data loading
    private async Task<Cart?> LoadCartAsync(Guid? userId, string? guestId, CancellationToken ct)
    {
        return await _context.Carts
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
                    .ThenInclude(a => a.ProductAttribute)
            .Include(c => c.Items)
                .ThenInclude(i => i.Attributes)
                    .ThenInclude(a => a.ProductAttributeValue)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<CartDto> BuildCartDtoAsync(Cart cartEntity, CancellationToken ct)
    {
        var cartDto = cartEntity.Adapt<CartDto>();

        // Fill line-level selected attributes (what the user chose for this cart item)
        foreach (var itemEntity in cartEntity.Items)
        {
            var dto = cartDto.Items.First(i => i.Id == itemEntity.Id);

            dto.SelectedAttributes = itemEntity.Attributes
                .Select(a => new CartItemAttributeDto
                {
                    AttributeId = a.ProductAttributeId,
                    AttributeName = a.ProductAttribute?.Name ?? string.Empty,
                    ValueId = a.ProductAttributeValueId,
                    Value = a.ProductAttributeValue?.Value
                })
                .ToList();

            // NEW: Map all product images (URLs) for this cart item
            dto.ImageUrls = itemEntity.Product?.Images?
                .Select(img => img.Path)           // adjust property name if different (e.g., Path)
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .ToList() ?? new List<string>();
        }

        // Load all product attribute mappings (available options) once
        var productIds = cartEntity.Items.Select(i => i.ProductId).Distinct().ToList();

        var productMappings = await _context.ProductAttributeMappings
            .AsNoTracking()
            .Where(m => productIds.Contains(m.ProductId))
            .Include(m => m.ProductAttribute)
            .Include(m => m.ProductAttributeValue)
            .Select(m => new
            {
                m.ProductId,
                Attr = new CartItemAttributeDto
                {
                    AttributeId = m.ProductAttributeId,
                    AttributeName = m.ProductAttribute.Name,
                    ValueId = m.ProductAttributeValueId,
                    Value = m.ProductAttributeValue != null ? m.ProductAttributeValue.Value : null
                }
            })
            .ToListAsync(ct);

        var byProduct = productMappings
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(v => v.Attr).ToList());

        // Assign available attribute options per product (without overwriting selected line attributes)
        foreach (var item in cartDto.Items)
        {
            item.ProductAttributes = byProduct.TryGetValue(item.ProductId, out var attrs)
                ? attrs
                : new List<CartItemAttributeDto>();
        }

        return cartDto;
    }

    private async Task<List<ProductSetting>> LoadActiveProductSettingsAsync(
        List<Guid> productIds,
        DateTimeOffset now,
        CancellationToken ct)
    {
        return await _context.ProductSettings
            .Include(ps => ps.Products)
            .Where(ps => ps.IsActive
                         && ps.StartDate <= now
                         && ps.EndDate >= now
                         && (ps.AppliesToAllProducts || ps.Products.Any(p => productIds.Contains(p.Id))))
            .ToListAsync(ct);
    }

    // Calculations
    private decimal ComputeItemDiscount(Cart cartEntity, List<ProductSetting> activeSettings)
    {
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

            var lineSubtotal = item.Product.Price * item.Quantity;
            if (productDiscount > lineSubtotal)
                productDiscount = lineSubtotal;

            itemDiscount += productDiscount;
        }

        return itemDiscount;
    }

    private Guid? GetShippingUserId(Cart cartEntity)
    {
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

        return sellerIds.FirstOrDefault();
    }

    private static CheckoutSummaryDto CreateEmptySummary() => new()
    {
        Cart = new CartDto(),
        SubTotal = 0,
        ShippingTotal = 0,
        Total = 0,
        Coupons = new List<CouponDto>()
    };

    private static CheckoutSummaryDto BuildSummary(
        CartDto cartDto,
        decimal subTotal,
        decimal itemDiscount,
        decimal shippingDiscount,
        decimal discountTotal,
        ShippingMethodSummaryDto? methodSummary,
        List<CouponDto> coupons)
    {
        return new CheckoutSummaryDto
        {
            Cart = cartDto,
            SubTotal = subTotal,
            ItemDiscount = itemDiscount,
            ShippingDiscount = shippingDiscount,
            DiscountTotal = discountTotal,
            ShippingTotal = methodSummary?.EffectiveCost ?? 0m,
            Total = subTotal - discountTotal + (methodSummary?.EffectiveCost ?? 0m),
            ShippingMethodId = methodSummary?.Id,
            FreeShippingApplied = methodSummary?.FreeShippingApplied ?? false,
            SelectedShippingMethod = methodSummary,
            Coupons = coupons
        };
    }

    private async Task<List<CouponDto>> GetValidCouponsForCurrentUserAsync(Product Product, Guid? userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

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
                RemainingPerUserUses = null
            })
            .ToListAsync(ct);

        if (userId is null)
            return baseCoupons;

        var userKey = userId.Value.ToString();
        var couponIds = baseCoupons.Select(b => b.Id).ToList();

        var perUserUsage = await _context.CouponUsages
            .AsNoTracking()
            .Where(u => u.UserId == userKey && couponIds.Contains(u.CouponId))
            .GroupBy(u => u.CouponId)
            .Select(g => new { CouponId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var perUserDict = perUserUsage.ToDictionary(x => x.CouponId, x => x.Count);

        var result = new List<CouponDto>(baseCoupons.Count);
        foreach (var c in baseCoupons)
        {
            if (c.PerUserLimit.HasValue)
            {
                var used = perUserDict.TryGetValue(c.Id, out var cnt) ? cnt : 0;
                var remaining = Math.Max(c.PerUserLimit.Value - used, 0);
                c.RemainingPerUserUses = remaining;

                if (remaining <= 0)
                    continue;
            }

            result.Add(c);
        }

        return result;
    }

    private async Task<ShippingMethodSummaryDto?> ResolveShippingAsync(Guid? userId, decimal subTotal, CancellationToken ct)
    {
        var method = await _context.ShippingMethods.FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault, ct);
        if (method is null) return null;

        var freeApplied = method.FreeShippingThreshold.HasValue && subTotal >= method.FreeShippingThreshold.Value;

        decimal effectiveCost = freeApplied
            ? 0m
            : method.CostType switch
            {
                ShippingCostType.Flat => method.Cost,
                ShippingCostType.ByTotal => Math.Round(subTotal * (method.Cost / 100m), 2, MidpointRounding.AwayFromZero),
                ShippingCostType.ByWeight => method.Cost,
                _ => method.Cost
            };

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