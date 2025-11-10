using ECommerce.Application.Common;
using ECommerce.Application.Features.Products.Extensions;
using ECommerce.Infrastructure.Persistence;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Products.Queries.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly ApplicationDbContext _context;

    public GetProductsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(p => p.NameEn.Contains(request.Search) || p.NameAr.Contains(request.Search) || p.SKU.Contains(request.Search));
        }

        query = query
            .FilterByCategory(request.CategoryId)
            .FilterByPrice(request.MinPrice, request.MaxPrice)
            .FilterByColor(request.Color)
            .OrderByBestRating(request.BestRating);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectToType<ProductDto>()
            .ToListAsync(cancellationToken);

        var paged = PagedResult<ProductDto>.Create(items, total, request.PageNumber, request.PageSize);
        return Result<PagedResult<ProductDto>>.Success(paged);
    }
}
