using ECommerce.Application.Common;
using ECommerce.Application.Orders.Commands.CheckoutCommand.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
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
            // Load user cart with items and products
            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == CurrentUser.Id, cancellationToken);

            if (cart is null || cart.Items.Count == 0)
                return Result<Guid>.Failure("Cart is empty.");

            // Resolve shipping address: either existing by Id or create from DTO
            var addressResult = await ResolveOrCreateAddressAsync(request, cancellationToken);
            if (!addressResult.Succeeded)
                return Result<Guid>.Failure("Invalid shipping address.");

            var address = addressResult.Data!;

            // Optional: load and validate coupon
            Coupon? coupon = null;
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                coupon = await _context.Set<Coupon>()
                    .FirstOrDefaultAsync(c => c.Code == request.CouponCode, cancellationToken);

                var now = DateTime.UtcNow;
                if (coupon is null || !coupon.IsActive || coupon.StartDate > now || coupon.EndDate < now)
                    return Result<Guid>.Failure("Invalid or expired coupon.");

                if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
                    return Result<Guid>.Failure("Coupon usage limit reached.");
            }

            // Preload allowed attribute mappings for involved products (for validation + snapshot names)
            var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();

            var allowedMappings = await _context.ProductAttributeMappings
                .Where(m => productIds.Contains(m.ProductId))
                .Select(m => new
                {
                    m.ProductId,
                    m.ProductAttributeId,
                    m.ProductAttributeValueId,
                    AttributeName = m.ProductAttribute.Name,
                    Value = m.ProductAttributeValue != null ? m.ProductAttributeValue.Value : null
                })
                .ToListAsync(cancellationToken);

            var allowedLookup = allowedMappings
                .GroupBy(a => a.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Validate stock and compute subtotal
            decimal subTotal = 0m;
            foreach (var item in cart.Items)
            {
                if (item.Product is null)
                    return Result<Guid>.Failure("One or more products no longer exist.");

                var available = item.Product.StockQuantity;
                var allowBackorder = item.Product.AllowBackorder;
                if (!allowBackorder && available < item.Quantity)
                    return Result<Guid>.Failure($"Insufficient stock for product '{item.Product.NameEn}'. Requested {item.Quantity}, available {available}.");

                var unitPrice = item.Product.Price;
                subTotal += unitPrice * item.Quantity;
            }

            // Apply coupon discount
            var discount = 0m;
            if (coupon is not null)
            {
                if (coupon.FixedAmount.HasValue)
                    discount += coupon.FixedAmount.Value;

                if (coupon.Percentage.HasValue && coupon.Percentage.Value > 0)
                    discount += Math.Round(subTotal * (coupon.Percentage.Value / 100m), 2, MidpointRounding.AwayFromZero);
            }

            if (discount < 0) discount = 0;
            if (discount > subTotal) discount = subTotal;

            // Resolve shipping method
            ShippingMethod? shippingMethod = null;

            if (request.ShippingMethodId.HasValue)
            {
                shippingMethod = await _context.ShippingMethods
                    .FirstOrDefaultAsync(sm => sm.Id == request.ShippingMethodId.Value, cancellationToken);

                if (shippingMethod is null)
                    return Result<Guid>.Failure("Selected shipping method not found.");
            }
            else
            {
                // Infer by address (CountryId/CityId -> zone -> default method)
                var zone = await _context.ShippingZones
                    .Include(z => z.Methods)
                    .FirstOrDefaultAsync(z =>
                        z.CountryId == address.CountryId &&
                        (z.CityId == null || z.CityId == address.CityId),
                        cancellationToken);

                shippingMethod = zone?.Methods.OrderByDescending(m => m.IsDefault).FirstOrDefault();
            }

            // Compute shipping cost with free-shipping logic
            decimal shippingCost = 0m;

            if (coupon?.FreeShipping == true)
            {
                shippingCost = 0m;
            }
            else if (shippingMethod is not null)
            {
                if (shippingMethod.FreeShippingThreshold.HasValue &&
                    subTotal >= shippingMethod.FreeShippingThreshold.Value)
                {
                    shippingCost = 0m;
                }
                else
                {
                    switch (shippingMethod.CostType)
                    {
                        case ShippingCostType.Flat:
                            shippingCost = shippingMethod.Cost;
                            break;
                        case ShippingCostType.ByTotal:
                            shippingCost = Math.Round(subTotal * (shippingMethod.Cost / 100m), 2, MidpointRounding.AwayFromZero);
                            break;
                        case ShippingCostType.ByWeight:
                            shippingCost = shippingMethod.Cost; // fallback (weights not modeled)
                            break;
                        default:
                            shippingCost = shippingMethod.Cost;
                            break;
                    }
                }
            }

            var total = subTotal - discount + shippingCost;

            // Normalize selections by CartItemId for quick access
            var selectionsByCartItemId = (request.ItemSelections ?? Array.Empty<OrderItemSelectionDto>())
                .ToDictionary(s => s.CartItemId, s => s.Attributes);

            // Create order with items and selected attributes
            var order = new Order
            {
                UserId = CurrentUser.Id,
                OrderNumber = GenerateOrderNumber(),
                Status = OrderStatus.Pending,

                SubTotal = subTotal,
                DiscountTotal = discount,
                ShippingTotal = shippingCost,
                Total = total,

                ShippingAddressId = address.Id,
                CouponCode = coupon?.Code,
                ShippingMethodId = shippingMethod?.Id ?? request.ShippingMethodId
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

                if (selectionsByCartItemId.TryGetValue(ci.Id, out var selectedAttrs) && selectedAttrs is not null && selectedAttrs.Count > 0)
                {
                    var byAttr = selectedAttrs.GroupBy(a => a.AttributeId).Select(g => g.First());

                    if (!allowedLookup.TryGetValue(ci.ProductId, out var allowedForProduct))
                        return Result<Guid>.Failure($"No attributes are defined for product '{ci.Product.NameEn}' but selections were supplied.");

                    foreach (var sel in byAttr)
                    {
                        var match = allowedForProduct.FirstOrDefault(a =>
                            a.ProductAttributeId == sel.AttributeId &&
                            a.ProductAttributeValueId == sel.ValueId);

                        if (match is null)
                        {
                            return Result<Guid>.Failure(
                                $"Invalid attribute selection for product '{ci.Product.NameEn}'. " +
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

            // Reserve/deduct stock if not allowing backorder
            foreach (var ci in cart.Items)
            {
                if (!ci.Product!.AllowBackorder)
                {
                    ci.Product.StockQuantity -= ci.Quantity;
                    if (ci.Product.StockQuantity < 0)
                        ci.Product.StockQuantity = 0;
                }
            }

            // Track coupon usage
            if (coupon is not null)
            {
                coupon.TimesUsed += 1;
            }

            _context.Orders.Add(order);

            // Clear cart after creating the order
            cart.Items.Clear();

            await _context.SaveChangesAsync(cancellationToken);
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

    private async Task<Result<UserAddress>> ResolveOrCreateAddressAsync(CheckoutCommand request, CancellationToken ct)
    {
        // Case 1: Existing address by Id
        if (request.ShippingAddressId.HasValue)
        {
            var addr = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == request.ShippingAddressId.Value && a.UserId == CurrentUser.Id, ct);

            if (addr is null)
                return Result<UserAddress>.Failure("Shipping address not found.");

            return Result<UserAddress>.Success(addr);
        }

        // Case 2: Create from NewAddress DTO
        if (request.NewAddress is not null)
        {
            // Basic validation
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

            // Ensure City belongs to Country
            var city = await _context.Cities.FirstOrDefaultAsync(c => c.Id == request.NewAddress.CityId, ct);
            if (city is null || city.CountryId != request.NewAddress.CountryId)
                return Result<UserAddress>.Failure("Invalid CityId/CountryId combination.");

            var entity = new UserAddress
            {
                UserId = CurrentUser.Id,
                FullName = request.NewAddress.FullName,
                CountryId = request.NewAddress.CountryId,
                CityId = request.NewAddress.CityId,
                Street = request.NewAddress.Street,
                MobileNumber = request.NewAddress.MobileNumber,
                HouseNo = request.NewAddress.HouseNo,
                IsDefault = request.NewAddress.IsDefault
            };

            // If marking as default, unset any previous defaults for this user
            if (entity.IsDefault)
            {
                var previousDefaults = await _context.UserAddresses
                    .Where(a => a.UserId == CurrentUser.Id && a.IsDefault)
                    .ToListAsync(ct);

                foreach (var a in previousDefaults)
                    a.IsDefault = false;
            }

            _context.UserAddresses.Add(entity);
            await _context.SaveChangesAsync(ct);

            return Result<UserAddress>.Success(entity);
        }

        return Result<UserAddress>.Failure("Either ShippingAddressId or NewAddress must be provided.");
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}