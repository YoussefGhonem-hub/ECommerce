using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using ECommerce.Shared.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Wishlist.Commands;

// Authenticated user resolved from CurrentUser.UserId; guest identified by GuestId.
public sealed record AddToWishlistCommand(Guid ProductId, string? GuestId) : IRequest<Result<bool>>;

public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public AddToWishlistCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.UserId; // Guid? (null if guest/unauthenticated)

        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(request.GuestId))
        {
            return Result<bool>.Validation(new()
            {
                { "Identity", new[] { "Either authenticated user or guestId must be provided." } }
            });
        }

        var productExists = await _context.Products
            .AnyAsync(p => p.Id == request.ProductId, cancellationToken);

        if (!productExists)
            return Result<bool>.Failure("Product.NotFound");

        var exists = await _context.FavoriteProducts
            .AnyAsync(f =>
                f.ProductId == request.ProductId &&
                (
                    (!string.IsNullOrWhiteSpace(userId) && f.UserId == userId.ToGuid()) ||
                    (string.IsNullOrWhiteSpace(userId) && f.GuestId == request.GuestId)
                ), cancellationToken);

        if (!exists)
        {
            _context.FavoriteProducts.Add(new FavoriteProduct
            {
                ProductId = request.ProductId,
                UserId = userId.ToGuid(),                                // null for guest
                GuestId = string.IsNullOrWhiteSpace(userId) ? null : request.GuestId
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true);
    }
}