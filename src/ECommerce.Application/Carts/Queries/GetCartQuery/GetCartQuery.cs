using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Carts.Queries.GetCartQuery;

public record GetCartQuery() : IRequest<Result<List<CartDto>>>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<List<CartDto>>>
{
    private readonly ApplicationDbContext _context;

    public GetCartQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CartDto>>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id;
        var guestId = CurrentUser.GuestId;

        // Includes cover: Items, Product, Category, Images, Reviews, Favorites, and Selected Attributes
        var carts = await _context.Carts
            .AsNoTracking()
            .Where(c =>
                (userId != null && c.UserId == userId) ||
                (guestId != null && c.GuestId == guestId))
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(i => i.FavoriteProducts)
            .Include(c => c.Items)
                .ThenInclude(i => i.Attributes)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product!.Category)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product!.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product!.ProductReviews)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product!.FavoriteProducts)
            .ProjectToType<CartDto>()
            .ToListAsync(cancellationToken);

        return Result<List<CartDto>>.Success(carts);
    }
}