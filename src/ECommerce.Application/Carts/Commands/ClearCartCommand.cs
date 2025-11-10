using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Carts.Commands;

public record ClearCartCommand(string? UserId, string? GuestId) : IRequest<Result<bool>>;

public class ClearCartHandler : IRequestHandler<ClearCartCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public ClearCartHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c =>
                (request.UserId != null && c.UserId == CurrentUser.Id) ||
                (request.GuestId != null && c.GuestId == request.GuestId),
                cancellationToken);

        if (cart is null) return Result<bool>.Success(true);

        cart.Items.Clear();
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}