using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDetailsDto>>
{
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
        var product = await LoadProductAsync(request.Id, cancellationToken);
        if (product is null)
            return Result<ProductDetailsDto>.Failure("Product.NotFound");

        product.Attributes = await LoadAttributesAsync(request.Id, cancellationToken);

        var userId = CurrentUser.Id;
        var guestId = CurrentUser.GuestId;

        product.IsInCart = await IsInCartAsync(request.Id, userId, guestId, cancellationToken);
        product.IsInWishlist = await IsInWishlistAsync(request.Id, userId, guestId, cancellationToken);

        // Populate related products with IsInWishlist/IsInCart flags
        product.RelatedProducts = await LoadRelatedProductsAsync(request.Id, userId, guestId, cancellationToken);

        return Result<ProductDetailsDto>.Success(product);
    }

    private Task<ProductDetailsDto?> LoadProductAsync(Guid id, CancellationToken ct) =>
        _context.Products
            .Include(x => x.ProductReviews)
            .Include(x => x.Images)
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ProjectToType<ProductDetailsDto>()
            .FirstOrDefaultAsync(ct);

    private Task<List<ProductAttributeMappingDto>> LoadAttributesAsync(Guid productId, CancellationToken ct) =>
        _context.ProductAttributeMappings
            .AsNoTracking()
            .Where(m => m.ProductId == productId)
            .Select(m => new ProductAttributeMappingDto
            {
                AttributeId = m.ProductAttributeId,
                AttributeName = m.ProductAttribute.Name,
                ValueId = m.ProductAttributeValueId,
                Value = m.ProductAttributeValue != null ? m.ProductAttributeValue.Value : null
            })
            .ToListAsync(ct);

    private Task<bool> IsInCartAsync(Guid productId, Guid? userId, string? guestId, CancellationToken ct) =>
        _context.Carts
            .AsNoTracking()
            .AnyAsync(c =>
                ((userId != null && c.UserId == userId) || (guestId != null && c.GuestId == guestId)) &&
                c.Items.Any(i => i.ProductId == productId),
                ct);

    private Task<bool> IsInWishlistAsync(Guid productId, Guid? userId, string? guestId, CancellationToken ct) =>
        _context.FavoriteProducts
            .AsNoTracking()
            .AnyAsync(fp =>
                fp.ProductId == productId &&
                (
                    (userId.HasValue && fp.UserId == userId) ||
                    (guestId != null && fp.GuestId == guestId)
                ),
                ct);

    private async Task<List<RelatedProductDto>> LoadRelatedProductsAsync(Guid productId, Guid? userId, string? guestId, CancellationToken ct)
    {
        // Base product info
        var baseInfo = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Id, p.CategoryId, p.Brand, p.Price })
            .FirstOrDefaultAsync(ct);

        if (baseInfo is null)
            return new List<RelatedProductDto>();

        // Distinct attribute ids of the base product
        var baseAttributeIds = await _context.ProductAttributeMappings
            .AsNoTracking()
            .Where(m => m.ProductId == baseInfo.Id)
            .Select(m => m.ProductAttributeId)
            .Distinct()
            .ToListAsync(ct);

        // Score + project fields needed (avoid navigating after materialization)
        var relatedRaw = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id != baseInfo.Id && p.CategoryId == baseInfo.CategoryId)
            .Select(p => new
            {
                p.Id,
                p.NameEn,
                p.NameAr,
                p.Price,
                p.Brand,
                MainImagePath = p.Images.Where(img => img.IsMain).Select(img => img.Path).FirstOrDefault(),
                Rating = p.AverageRating,
                Created = p.CreatedDate,
                BrandMatch = p.Brand != null && baseInfo.Brand != null && p.Brand == baseInfo.Brand,
                PriceDiff = Math.Abs(p.Price - baseInfo.Price),
                AttributeOverlap = baseAttributeIds.Count == 0
                    ? 0
                    : _context.ProductAttributeMappings
                        .Where(m => m.ProductId == p.Id && baseAttributeIds.Contains(m.ProductAttributeId))
                        .Select(m => m.ProductAttributeId)
                        .Distinct()
                        .Count()
            })
            .OrderByDescending(x => x.BrandMatch)
            .ThenByDescending(x => x.AttributeOverlap)
            .ThenBy(x => x.PriceDiff)
            .ThenByDescending(x => x.Rating)
            .ThenByDescending(x => x.Created)
            .Take(8)
            .ToListAsync(ct);

        if (relatedRaw.Count == 0)
            return new List<RelatedProductDto>();

        var relatedIds = relatedRaw.Select(r => r.Id).ToList();

        // Bulk flags: InCart and InWishlist sets
        var cartProductIds = await _context.Carts
            .AsNoTracking()
            .Where(c =>
                (userId != null && c.UserId == userId) ||
                (guestId != null && c.GuestId == guestId))
            .SelectMany(c => c.Items.Select(i => i.ProductId))
            .Where(pid => relatedIds.Contains(pid))
            .Distinct()
            .ToListAsync(ct);

        var wishlistProductIds = await _context.FavoriteProducts
            .AsNoTracking()
            .Where(fp =>
                relatedIds.Contains(fp.ProductId.Value) &&
                (
                    (userId.HasValue && fp.UserId == userId) ||
                    (guestId != null && fp.GuestId == guestId)
                ))
            .Select(fp => fp.ProductId)
            .Distinct()
            .ToListAsync(ct);

        var cartSet = cartProductIds.Count > 0 ? cartProductIds.ToHashSet() : new HashSet<Guid>();
        var wishSet = wishlistProductIds.Count > 0 ? wishlistProductIds.ToHashSet() : null;

        // Build DTOs
        var list = relatedRaw.Select(x => new RelatedProductDto
        {
            Id = x.Id,
            NameEn = x.NameEn,
            NameAr = x.NameAr,
            Price = x.Price,
            Brand = x.Brand,
            MainImagePath = x.MainImagePath
                ?? "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500",
            AverageRating = x.Rating,
            IsInCart = cartSet.Contains(x.Id),
            IsInWishlist = wishSet == null ? false : wishSet.Contains(x.Id)
        }).ToList();

        return list;
    }
}