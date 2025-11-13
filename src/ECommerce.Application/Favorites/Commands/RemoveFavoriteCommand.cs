using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Wishlist.Commands;

// Auth context: use CurrentUser.UserId if authenticated; fall back to GuestId.
public sealed record RemoveFromWishlistCommand(Guid ProductId, string? GuestId) : IRequest<Result<bool>>;

public class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public RemoveFromWishlistCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.UserId;

        if (!string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(request.GuestId))
        {
            return Result<bool>.Validation(new()
            {
                { "Identity", new[] { "Either authenticated user or guestId must be provided." } }
            });
        }

        // Get existing favorite for this identity
        var favorite = await _context.FavoriteProducts
            .FirstOrDefaultAsync(f =>
                f.ProductId == request.ProductId &&
                (
                    string.IsNullOrWhiteSpace(userId) ||
                    (!string.IsNullOrWhiteSpace(userId) && f.GuestId == request.GuestId)
                ),
                cancellationToken);

        if (favorite is not null)
        {
            _context.FavoriteProducts.Remove(favorite);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Removed (or was already absent) -> not in wishlist
        return Result<bool>.Success(false);
    }
}