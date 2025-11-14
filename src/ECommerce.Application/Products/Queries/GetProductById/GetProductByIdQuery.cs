using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDetailsDto>>
{
    // Optional guest context for wishlist detection when unauthenticated
    public string? GuestId { get; init; }
}

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDetailsDto>>
{
    private readonly ApplicationDbContext _context;

    public GetProductByIdHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProductDetailsDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        // 1) Project main product details with images and approved reviews using Mapster (EF-translated)
        var product = await _context.Products
            .Include(x => x.ProductReviews)
            .Include(x => x.Images)
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .ProjectToType<ProductDetailsDto>()
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
            return Result<ProductDetailsDto>.Failure("Product.NotFound");

        // 2) Load attribute mappings separately (no inverse nav on Product)
        var attributes = await _context.ProductAttributeMappings
            .AsNoTracking()
            .Where(m => m.ProductId == request.Id)
            .Select(m => new ProductAttributeMappingDto
            {
                AttributeId = m.ProductAttributeId,
                AttributeName = m.ProductAttribute.Name,
                ValueId = m.ProductAttributeValueId,
                Value = m.ProductAttributeValue != null ? m.ProductAttributeValue.Value : null
            })
            .ToListAsync(cancellationToken);

        product.Attributes = attributes;

        // 3) Determine wishlist status for current user or guest
        var userId = CurrentUser.Id; // Guid? (null when unauthenticated)
        product.IsInWishlist = await _context.FavoriteProducts
            .AsNoTracking()
            .AnyAsync(fp =>
                fp.ProductId == request.Id &&
                (
                    (userId.HasValue && fp.UserId == userId) ||
                    (fp.GuestId == CurrentUser.GuestId)
                ),
                cancellationToken);

        return Result<ProductDetailsDto>.Success(product);
    }
}
