using ECommerce.Application.Common;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Queries.GetProducts;

public class GetProductsQuery : IRequest<Result<PagedResult<ProductDto>>>
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly Infrastructure.Persistence.ApplicationDbContext _context;

    public GetProductsHandler(Infrastructure.Persistence.ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Normalize paging
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var query = _context.Products
            .Include(x => x.Category)
            .AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.NameAr.Contains(term) ||
                p.NameEn.Contains(term) ||
                p.SKU.Contains(term) ||
                (p.Brand != null && p.Brand.Contains(term)));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        // Total count before paging
        var totalCount = await query.CountAsync(cancellationToken);

        // Page slice
        var items = await query
            .OrderByDescending(p => p.CreatedDate)  // stable ordering
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectToType<ProductDto>()            // Mapster projection (translated by EF)
            .ToListAsync(cancellationToken);

        var paged = PagedResult<ProductDto>.Create(items, totalCount, page, pageSize);
        return Result<PagedResult<ProductDto>>.Success(paged);
    }
}
