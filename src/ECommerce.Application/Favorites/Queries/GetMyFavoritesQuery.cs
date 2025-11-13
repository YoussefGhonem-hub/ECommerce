using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Favorites.Queries;

// For authenticated users we ignore GuestId and use CurrentUser.UserId.
// If unauthenticated (no CurrentUser.UserId) we require GuestId.
public sealed record GetMyFavoritesQuery(string? GuestId) : IRequest<Result<List<FavoriteProduct>>>;

public class GetMyFavoritesHandler : IRequestHandler<GetMyFavoritesQuery, Result<List<FavoriteProduct>>>
{
    private readonly ApplicationDbContext _context;

    public GetMyFavoritesHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<FavoriteProduct>>> Handle(GetMyFavoritesQuery request, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id; // Guid? (null if guest)
        var isAuthenticated = userId.HasValue;

        if (!isAuthenticated && string.IsNullOrWhiteSpace(request.GuestId))
        {
            return Result<List<FavoriteProduct>>.Validation(new()
            {
                { "Identity", new[] { "GuestId is required when user is not authenticated." } }
            });
        }

        try
        {
            IQueryable<FavoriteProduct> query = _context.FavoriteProducts
                .AsNoTracking()
                .Include(f => f.Product);

            query = isAuthenticated
                ? query.Where(f => f.UserId == userId)
                : query.Where(f => f.GuestId == request.GuestId);

            var favorites = await query
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync(cancellationToken);

            return Result<List<FavoriteProduct>>.Success(favorites);
        }
        catch (OperationCanceledException)
        {
            return Result<List<FavoriteProduct>>.Failure("Operation was cancelled.");
        }
        catch (Exception ex)
        {
            return Result<List<FavoriteProduct>>.Failure("Failed to retrieve favorites.", ex.Message);
        }
    }
}
