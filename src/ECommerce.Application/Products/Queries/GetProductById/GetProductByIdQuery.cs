using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDetailsDto>>;

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

        // 3) Final safeguards: ensure main image fallback and recalc average if needed
        if (string.IsNullOrWhiteSpace(product.MainImagePath))
        {
            product.MainImagePath = "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500";
        }

        if (product.AverageRating <= 0 && product.Reviews.Count > 0)
        {
            product.AverageRating = product.Reviews.Average(r => r.Rating);
        }

        return Result<ProductDetailsDto>.Success(product);
    }
}
