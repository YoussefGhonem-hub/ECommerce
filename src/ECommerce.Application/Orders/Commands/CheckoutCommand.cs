using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Orders.Commands;

public record CheckoutCommand(Guid UserId, Guid ShippingAddressId, string? CouponCode = null, Guid? ShippingMethodId = null) : IRequest<Result<Guid>>;

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
                // NOTE: Requires DbSet<Coupon> on IApplicationDbContext
                coupon = await _context.Set<Coupon>()
                    .FirstOrDefaultAsync(c => c.Code == request.CouponCode, cancellationToken);

                var now = DateTime.UtcNow;
                if (coupon is null || !coupon.IsActive || coupon.StartDate > now || coupon.EndDate < now)
                    return Result<Guid>.Failure("Invalid or expired coupon.");

                if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
                    return Result<Guid>.Failure("Coupon usage limit reached.");
            }

            // Validate stock and compute totals; set unit prices from current product price
            decimal subTotal = 0m;
            foreach (var item in cart.Items)
            {
                if (item.Product is null)
                    return Result<Guid>.Failure("One or more products no longer exist.");

                // Stock check (allow backorder if configured)
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

            // Create order
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

            order.Items = cart.Items.Select(ci =>
            {
                var unitPrice = ci.Product!.Price;
                var lineTotal = unitPrice * ci.Quantity;

                return new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = unitPrice,
                };
            }).ToList();

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
