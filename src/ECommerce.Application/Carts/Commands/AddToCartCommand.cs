using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Carts.Commands;

public record AddToCartCommand(Guid? UserId, string? GuestId, Guid ProductId, int Quantity) : IRequest<Result<bool>>;

public class AddToCartHandler : IRequestHandler<AddToCartCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public AddToCartHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c =>
                (request.UserId != null && c.UserId == request.UserId) ||
                (request.GuestId != null && c.GuestId == request.GuestId),
                cancellationToken);

        if (cart is null)
        {
            cart = new Cart
            {
                UserId = request.UserId,
                GuestId = request.GuestId
            };
            _context.Carts.Add(cart);
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity
            });
        }
        else
        {
            existingItem.Quantity += request.Quantity;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}