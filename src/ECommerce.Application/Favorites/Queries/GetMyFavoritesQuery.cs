using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Favorites.Queries;

public sealed record GetMyFavoritesQuery(string UserId) : IRequest<Result<List<FavoriteProduct>>>;

public class GetMyFavoritesHandler : IRequestHandler<GetMyFavoritesQuery, Result<List<FavoriteProduct>>>
{
    private readonly ApplicationDbContext _context;

    public GetMyFavoritesHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<FavoriteProduct>>> Handle(GetMyFavoritesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<List<FavoriteProduct>>.Validation(new()
            {
                { nameof(request.UserId), new[] { "UserId is required." } }
            });
        }

        try
        {
            var favorites = await _context.FavoriteProducts
                .AsNoTracking()
                .Include(f => f.Product)
                .Where(f => f.UserId == CurrentUser.Id)
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
