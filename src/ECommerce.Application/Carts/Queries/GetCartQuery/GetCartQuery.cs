using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Carts.Queries.GetCartQuery;

public record GetCartQuery(Guid? UserId, string? GuestId) : IRequest<Result<CartDto>>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    private readonly ApplicationDbContext _context;

    public GetCartQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c =>
                (request.UserId != null && c.UserId == request.UserId) ||
                (request.GuestId != null && c.GuestId == request.GuestId),
                cancellationToken);

        if (cart is null)
            return Result<CartDto>.Success(new CartDto());

        var dto = new CartDto
        {
            Id = cart.Id,
            Items = cart.Items.Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.NameEn ?? i.Product?.NameAr ?? string.Empty,
                Price = i.Product?.Price ?? 0,
                Quantity = i.Quantity
            }).ToList()
        };

        return Result<CartDto>.Success(dto);
    }
}
