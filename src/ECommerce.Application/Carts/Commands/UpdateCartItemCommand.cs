using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Carts.Commands;

public record UpdateCartItemCommand(string? UserId, string? GuestId, Guid CartItemId, int Quantity) : IRequest<Result<bool>>;

public class UpdateCartItemHandler : IRequestHandler<UpdateCartItemCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public UpdateCartItemHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
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

        item.Quantity = request.Quantity;
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
