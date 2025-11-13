using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Orders.Commands;

// Add optional attribute selections per cart item
public record CheckoutCommand(
    Guid UserId,
    Guid ShippingAddressId,
    string? CouponCode = null,
    Guid? ShippingMethodId = null,
    IReadOnlyList<OrderItemSelectionDto>? ItemSelections = null
) : IRequest<Result<Guid>>;

// Per-cart-item attribute selections
public record OrderItemSelectionDto(Guid CartItemId, IReadOnlyList<SelectedAttributeDto> Attributes);
// Attribute + optional value (for attributes with a predefined value list)
public record SelectedAttributeDto(Guid AttributeId, Guid? ValueId);

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CheckoutCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        // Basic validation
        if (request.UserId == Guid.Empty)
        {
            return Result<Guid>.Validation(new()
            {
                { nameof(request.UserId), new[] { "UserId is required." } }
            });
        }

        try
        {
            // Load user cart with items and products
            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

            if (cart is null || cart.Items.Count == 0)
                return Result<Guid>.Failure("Cart is empty.");

            // Validate shipping address ownership
            var addressExists = await _context.UserAddresses
                .AnyAsync(a => a.Id == request.ShippingAddressId && a.UserId == request.UserId, cancellationToken);

            if (!addressExists)
                return Result<Guid>.Failure("Shipping address not found.");

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

            // Build a lookup to speed up validation
            var allowedLookup = allowedMappings
                .GroupBy(a => a.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );

            // Validate stock and compute totals; set unit prices from current product price
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

            // Apply coupon
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

            // Shipping cost (basic). If you have ShippingMethod entity, load and use it here.
            var shippingCost = 0m;
            if (coupon?.FreeShipping == true)
                shippingCost = 0m;

            var total = subTotal - discount + shippingCost;

            // Normalize selections by CartItemId for quick access
            var selectionsByCartItemId = (request.ItemSelections ?? Array.Empty<OrderItemSelectionDto>())
                .ToDictionary(s => s.CartItemId, s => s.Attributes);

            // Create order with items and selected attributes
            var order = new Order
            {
                UserId = request.UserId,
                OrderNumber = GenerateOrderNumber(),
                Status = OrderStatus.Pending,
                Total = total,
                ShippingAddressId = request.ShippingAddressId,
                CouponCode = coupon?.Code,
                ShippingMethodId = request.ShippingMethodId
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

                // Attach selected attributes (if provided) and validate against allowed mappings for this product
                if (selectionsByCartItemId.TryGetValue(ci.Id, out var selectedAttrs) && selectedAttrs is not null && selectedAttrs.Count > 0)
                {
                    // Avoid duplicates by AttributeId (keep first occurrence)
                    var byAttr = selectedAttrs
                        .GroupBy(a => a.AttributeId)
                        .Select(g => g.First());

                    if (!allowedLookup.TryGetValue(ci.ProductId, out var allowedForProduct))
                        return Result<Guid>.Failure($"No attributes are defined for product '{ci.Product.NameEn}' but selections were supplied.");

                    foreach (var sel in byAttr)
                    {
                        // Validate pair (AttributeId, ValueId) belongs to this product
                        var match = allowedForProduct.FirstOrDefault(a =>
                            a.ProductAttributeId == sel.AttributeId &&
                            a.ProductAttributeValueId == sel.ValueId);

                        if (match is null)
                        {
                            return Result<Guid>.Failure(
                                $"Invalid attribute selection for product '{ci.Product.NameEn}'. " +
                                $"AttributeId={sel.AttributeId}, ValueId={(sel.ValueId?.ToString() ?? "null")} is not allowed.");
                        }

                        oi.Attributes.Add(new OrderItemAttribute
                        {
                            ProductAttributeId = sel.AttributeId,
                            ProductAttributeValueId = sel.ValueId,
                            AttributeName = match.AttributeName, // snapshot
                            Value = match.Value                   // snapshot (may be null)
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

    private static string GenerateOrderNumber()
    {
        // Simple, sortable order number. Adjust to your preferred format.
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}