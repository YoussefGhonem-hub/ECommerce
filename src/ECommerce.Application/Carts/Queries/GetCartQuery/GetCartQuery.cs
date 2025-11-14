using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Carts.Queries.GetCartQuery;

public record GetCartQuery() : IRequest<Result<CartDto>>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    private readonly ApplicationDbContext _context;

    public GetCartQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var userId = CurrentUser.Id;
        var guestId = CurrentUser.GuestId;

        // Single projection query (no N+1, no post materialization shaping)
        var cartDto = await _context.Carts
            .AsNoTracking()
            .Where(c =>
                (userId != null && c.UserId == userId) ||
                (guestId != null && c.GuestId == guestId))
            .Select(c => new CartDto
            {
                Id = c.Id,
                Items = c.Items.Select(i => new CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Product != null ? i.Product.Price : 0,
                    StockQuantity = i.Product != null ? i.Product.StockQuantity : 0,
                    ProductName = i.Product != null
                        ? (i.Product.NameEn ?? i.Product.NameAr ?? string.Empty)
                        : string.Empty,
                    Brand = i.Product != null ? i.Product.Brand : null,
                    CategoryNameEn = i.Product != null && i.Product.Category != null
                        ? i.Product.Category.NameEn
                        : string.Empty,
                    CategoryNameAr = i.Product != null && i.Product.Category != null
                        ? i.Product.Category.NameAr
                        : string.Empty,
                    MainImagePath = i.Product != null
                        ? i.Product.Images
                            .Where(img => img.IsMain)
                            .Select(img => img.Path)
                            .FirstOrDefault()
                            ?? "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500"
                        : "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500",
                    AverageRating = i.Product != null
                        ? (
                            i.Product.ProductReviews
                                .Where(r => r.IsApproved)
                                .Select(r => (double?)r.Rating)
                                .Average() ?? 0d
                          )
                        : 0d,
                    IsInWishlist = i.Product != null &&
                                   (
                                       i.Product.FavoriteProducts.Any(fp => userId != null && fp.UserId == userId) ||
                                       i.Product.FavoriteProducts.Any(fp => guestId != null && fp.GuestId == guestId)
                                   ),
                    IsInCart = true,
                    SelectedAttributes = i.Attributes
                        .Select(a => new CartItemAttributeDto
                        {
                            AttributeId = a.ProductAttributeId,
                            ValueId = a.ProductAttributeValueId,
                            AttributeName = a.AttributeName,
                            Value = a.Value
                        })
                        .ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cartDto is null)
            return Result<CartDto>.Success(new CartDto());

        return Result<CartDto>.Success(cartDto);
    }
}
