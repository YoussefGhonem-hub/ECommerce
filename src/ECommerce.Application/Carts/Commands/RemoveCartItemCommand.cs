using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Carts.Commands;

public record RemoveCartItemCommand(string? UserId, string? GuestId, Guid CartItemId) : IRequest<Result<bool>>;

public class RemoveCartItemHandler : IRequestHandler<RemoveCartItemCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public RemoveCartItemHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c =>
                (request.UserId != null && c.UserId == CurrentUser.Id) ||
                (request.GuestId != null && c.GuestId == request.GuestId),
                cancellationToken);

        if (cart is null) return Result<bool>.Failure("Cart not found");

        var item = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId);
        if (item is null) return Result<bool>.Failure("Item not found");

        cart.Items.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}