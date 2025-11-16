using ECommerce.Application.Common;
using ECommerce.Application.Orders.Commands.CheckoutCommand.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using ECommerce.Shared.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Orders.Commands.CheckoutCommand;

public record CheckoutCommand(
    Guid? ShippingAddressId,
    string? CouponCode = null,
    Guid? ShippingMethodId = null,
    IReadOnlyList<OrderItemSelectionDto>? ItemSelections = null,
    UserAddressDto? NewAddress = null
) : IRequest<Result<Guid>>;

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CheckoutCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        if (CurrentUser.Id == Guid.Empty)
        {
            return Result<Guid>.Validation(new()
            {
                { nameof(CurrentUser.Id), new[] { "UserId is required." } }
            });
        }

        try
        {
            var cart = await LoadUserCartAsync(cancellationToken);
            if (cart is null || cart.Items.Count == 0)
                return Result<Guid>.Failure("Cart is empty.");

            // 2) Resolve or create address
            var addressResult = await ResolveOrCreateAddressAsync(request, cancellationToken);
            if (!addressResult.Succeeded)
                return Result<Guid>.Failure("Invalid shipping address.");
            var address = addressResult.Data!;

            // 3) Validate coupon (optional)
            var couponResult = await ValidateAndGetCouponAsync(request.CouponCode, cancellationToken);
            if (!couponResult.Succeeded)
                return Result<Guid>.Failure("Invalid or expired coupon.");
            var coupon = couponResult.Data;

            // 4) Prepare attribute mappings for validation
            var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
            var allowedLookup = await PreloadAllowedAttributeMappingsAsync(productIds, cancellationToken);

            // 5) Validate stock and compute subtotal
            var subTotalResult = ValidateStockAndComputeSubtotal(cart);
            if (!subTotalResult.Succeeded)
                return Result<Guid>.Failure(subTotalResult.Errors?.FirstOrDefault() ?? "Invalid order items.");
            var subTotal = subTotalResult.Data;

            // 6) Compute discount
            var discount = ComputeDiscount(subTotal, coupon);

            // 7) Resolve shipping method
            var shippingMethodResult = await ResolveShippingMethodAsync(request, address, cancellationToken);
            if (!shippingMethodResult.Succeeded)
                return Result<Guid>.Failure("Selected shipping method not found.");
            var shippingMethod = shippingMethodResult.Data;

            // 8) Compute shipping cost
            var shippingCost = ComputeShippingCost(subTotal, coupon, shippingMethod);

            // 9) Build order (validates selected attributes)
            var orderResult = BuildOrder(cart, address, coupon, shippingMethod, subTotal, discount, shippingCost, request.ItemSelections, allowedLookup);
            if (!orderResult.Succeeded)
                return Result<Guid>.Failure(orderResult.Errors?.FirstOrDefault() ?? "Failed to build order.");
            var order = orderResult.Data!;

            // 10) Deduct stock and track coupon usage
            DeductStockIfNeeded(cart);
            TrackCouponUsage(coupon);

            // 11) Persist order and clear cart
            _context.Orders.Add(order);
            cart.Items.Clear();
            try
            {
                await _context.SaveChangesAsync(cancellationToken);

            }
            catch (Exception ex)
            {

                throw;
            }

            return Result<Guid>.Success(order.Id);
        }
        catch (OperationCanceledException)
        {
            return Result<Guid>.Failure("Operation was cancelled.");
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure("Checkout failed.", ex.Message);
        }
    }

    private async Task<Cart?> LoadUserCartAsync(CancellationToken ct)
    {
        return await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == CurrentUser.Id, ct);
    }

    private async Task<Result<Coupon?>> ValidateAndGetCouponAsync(string? couponCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
            return Result<Coupon?>.Success(null);

        var coupon = await _context.Set<Coupon>()
            .FirstOrDefaultAsync(c => c.Code == couponCode, ct);

        var now = DateTime.UtcNow;
        if (coupon is null || !coupon.IsActive || coupon.StartDate > now || coupon.EndDate < now)
            return Result<Coupon?>.Failure("Invalid or expired coupon.");
        if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
            return Result<Coupon?>.Failure("Coupon usage limit reached.");

        return Result<Coupon?>.Success(coupon);
    }

    private async Task<Dictionary<Guid, List<AllowedMapping>>> PreloadAllowedAttributeMappingsAsync(
        List<Guid> productIds,
        CancellationToken ct)
    {
        var allowedMappings = await _context.ProductAttributeMappings
            .Where(m => productIds.Contains(m.ProductId))
            .Select(m => new AllowedMapping
            {
                ProductId = m.ProductId,
                ProductAttributeId = m.ProductAttributeId,
                ProductAttributeValueId = m.ProductAttributeValueId,
                AttributeName = m.ProductAttribute.Name,
                Value = m.ProductAttributeValue != null ? m.ProductAttributeValue.Value : null
            })
            .ToListAsync(ct);

        return allowedMappings
            .GroupBy(a => a.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private Result<decimal> ValidateStockAndComputeSubtotal(Cart cart)
    {
        decimal subTotal = 0m;

        foreach (var item in cart.Items)
        {
            if (item.Product is null)
                return Result<decimal>.Failure("One or more products no longer exist.");

            var available = item.Product.StockQuantity;
            var allowBackorder = item.Product.AllowBackorder;
            if (!allowBackorder && available < item.Quantity)
                return Result<decimal>.Failure($"Insufficient stock for product '{item.Product.NameEn}'. Requested {item.Quantity}, available {available}.");

            subTotal += item.Product.Price * item.Quantity;
        }

        return Result<decimal>.Success(subTotal);
    }

    private decimal ComputeDiscount(decimal subTotal, Coupon? coupon)
    {
        if (coupon is null) return 0m;

        decimal discount = 0m;

        if (coupon.FixedAmount.HasValue)
            discount += coupon.FixedAmount.Value;

        if (coupon.Percentage.HasValue && coupon.Percentage.Value > 0)
            discount += Math.Round(subTotal * (coupon.Percentage.Value / 100m), 2, MidpointRounding.AwayFromZero);

        if (discount < 0) discount = 0;
        if (discount > subTotal) discount = subTotal;
        return discount;
    }

    private async Task<Result<ShippingMethod?>> ResolveShippingMethodAsync(
        CheckoutCommand request,
        UserAddress address,
        CancellationToken ct)
    {
        if (request.ShippingMethodId.HasValue)
        {
            var method = await _context.ShippingMethods
                .FirstOrDefaultAsync(sm => sm.Id == request.ShippingMethodId.Value, ct);

            if (method is null)
                return Result<ShippingMethod?>.Failure("Selected shipping method not found.");

            return Result<ShippingMethod?>.Success(method);
        }
        else
        {
            // Infer by address (CountryId/CityId -> zone -> default method)
            var zone = await _context.ShippingZones
                .Include(z => z.Methods)
                .FirstOrDefaultAsync(z =>
                    (z.CityId == null || z.CityId == address.CityId), ct);

            var method = zone?.Methods.OrderByDescending(m => m.IsDefault).FirstOrDefault();
            return Result<ShippingMethod?>.Success(method);
        }
    }

    private decimal ComputeShippingCost(decimal subTotal, Coupon? coupon, ShippingMethod? method)
    {
        if (coupon?.FreeShipping == true)
            return 0m;

        if (method is null)
            return 0m;

        if (method.FreeShippingThreshold.HasValue && subTotal >= method.FreeShippingThreshold.Value)
            return 0m;

        return method.CostType switch
        {
            ShippingCostType.Flat => method.Cost,
            ShippingCostType.ByTotal => Math.Round(subTotal * (method.Cost / 100m), 2, MidpointRounding.AwayFromZero),
            ShippingCostType.ByWeight => method.Cost, // weights not modeled
            _ => method.Cost
        };
    }

    private Result<Order> BuildOrder(
        Cart cart,
        UserAddress address,
        Coupon? coupon,
        ShippingMethod? shippingMethod,
        decimal subTotal,
        decimal discount,
        decimal shippingCost,
        IReadOnlyList<OrderItemSelectionDto>? itemSelections,
        Dictionary<Guid, List<AllowedMapping>> allowedLookup)
    {
        var selectionsByCartItemId = (itemSelections ?? Array.Empty<OrderItemSelectionDto>())
            .ToDictionary(s => s.CartItemId, s => s.Attributes);

        var order = new Order
        {
            UserId = CurrentUser.Id,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Pending,

            SubTotal = subTotal,
            DiscountTotal = discount,
            ShippingTotal = shippingCost,
            Total = subTotal - discount + shippingCost,

            ShippingAddress = null,
            ShippingAddressId = address.Id,
            CouponCode = coupon?.Code,
            ShippingMethodId = shippingMethod?.Id
        };

        var orderItems = new List<OrderItem>();

        foreach (var ci in cart.Items)
        {
            var unitPrice = ci.Product!.Price;

            var oi = new OrderItem
            {
                OrderId = order.Id,
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = unitPrice,
            };

            if (selectionsByCartItemId.TryGetValue(ci.Id, out var selectedAttrs) &&
                selectedAttrs is not null && selectedAttrs.Count > 0)
            {
                var byAttr = selectedAttrs.GroupBy(a => a.AttributeId).Select(g => g.First());

                if (!allowedLookup.TryGetValue(ci.ProductId, out var allowedForProduct))
                    return Result<Order>.Failure($"No attributes are defined for product '{ci.Product!.NameEn}' but selections were supplied.");

                foreach (var sel in byAttr)
                {
                    var match = allowedForProduct.FirstOrDefault(a =>
                        a.ProductAttributeId == sel.AttributeId &&
                        a.ProductAttributeValueId == sel.ValueId);

                    if (match is null)
                    {
                        return Result<Order>.Failure(
                            $"Invalid attribute selection for product '{ci.Product!.NameEn}'. " +
                            $"AttributeId={sel.AttributeId}, ValueId={sel.ValueId?.ToString() ?? "null"} is not allowed.");
                    }

                    oi.Attributes.Add(new OrderItemAttribute
                    {
                        ProductAttributeId = sel.AttributeId,
                        ProductAttributeValueId = sel.ValueId,
                        AttributeName = match.AttributeName,
                        Value = match.Value
                    });
                }
            }

            orderItems.Add(oi);
        }

        order.Items = orderItems;
        return Result<Order>.Success(order);
    }

    private void DeductStockIfNeeded(Cart cart)
    {
        foreach (var ci in cart.Items)
        {
            if (!ci.Product!.AllowBackorder)
            {
                ci.Product.StockQuantity -= ci.Quantity;
                if (ci.Product.StockQuantity < 0)
                    ci.Product.StockQuantity = 0;
            }
        }
    }

    private void TrackCouponUsage(Coupon? coupon)
    {
        if (coupon is not null)
            coupon.TimesUsed += 1;
    }

    private async Task<Result<UserAddress>> ResolveOrCreateAddressAsync(CheckoutCommand request, CancellationToken ct)
    {
        // Case 1: Existing address by Id
        if (request.ShippingAddressId.HasValue)
        {
            var addr = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == request.ShippingAddressId.Value && a.UserId == "12D864DC-1735-4C7B-E759-08DE241865BC".ToGuid(), ct);

            if (addr is null)
                return Result<UserAddress>.Failure("Shipping address not found.");

            return Result<UserAddress>.Success(addr);
        }

        // Case 2: Create from NewAddress DTO
        if (request.NewAddress is not null)
        {
            var errors = new Dictionary<string, string[]>();

            if (request.NewAddress.CountryId == Guid.Empty)
                errors[nameof(request.NewAddress.CountryId)] = new[] { "CountryId is required." };
            if (request.NewAddress.CityId == Guid.Empty)
                errors[nameof(request.NewAddress.CityId)] = new[] { "CityId is required." };
            if (string.IsNullOrWhiteSpace(request.NewAddress.Street))
                errors[nameof(request.NewAddress.Street)] = new[] { "Street is required." };
            if (string.IsNullOrWhiteSpace(request.NewAddress.FullName))
                errors[nameof(request.NewAddress.FullName)] = new[] { "FullName is required." };
            if (string.IsNullOrWhiteSpace(request.NewAddress.MobileNumber))
                errors[nameof(request.NewAddress.MobileNumber)] = new[] { "MobileNumber is required." };

            if (errors.Count > 0)
                return Result<UserAddress>.Validation(errors);

            // Ensure City exists + belongs to Country
            var city = await _context.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.NewAddress.CityId, ct);
            if (city is null || city.CountryId != request.NewAddress.CountryId)
                return Result<UserAddress>.Failure("Invalid CityId/CountryId combination.");

            var entity = new UserAddress
            {
                UserId = CurrentUser.Id,
                FullName = request.NewAddress.FullName,
                CityId = request.NewAddress.CityId,
                Street = request.NewAddress.Street,
                MobileNumber = request.NewAddress.MobileNumber,
                HouseNo = request.NewAddress.HouseNo,
                IsDefault = request.NewAddress.IsDefault
            };

            if (entity.IsDefault)
            {
                var previousDefaults = await _context.UserAddresses
                    .Where(a => a.UserId == CurrentUser.Id && a.IsDefault)
                    .ToListAsync(ct);

                foreach (var a in previousDefaults)
                    a.IsDefault = false;
            }

            _context.UserAddresses.Add(entity);

            try
            {
                await _context.SaveChangesAsync(ct);
                return Result<UserAddress>.Success(entity);
            }
            catch (DbUpdateException ex)
            {
                return Result<UserAddress>.Failure("Failed to create address. Please ensure CityId is valid.", ex.InnerException?.Message ?? ex.Message);
            }
        }

        return Result<UserAddress>.Failure("Either ShippingAddressId or NewAddress must be provided.");
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }

    private sealed class AllowedMapping
    {
        public Guid ProductId { get; set; }
        public Guid ProductAttributeId { get; set; }
        public Guid? ProductAttributeValueId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}