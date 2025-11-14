using ECommerce.Application.Common;
using ECommerce.Shared.CurrentUser;
using ECommerce.Shared.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Queries.GetProducts;

public class GetProductsQuery : BaseFilterDto, IRequest<Result<PagedResult<ProductDto>>>
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? Price { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
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
        var userId = CurrentUser.Id;
        var guestId = CurrentUser.GuestId;

        // Collect product ids currently in cart (single small query)
        var cartProductIds = await _context.Carts
            .AsNoTracking()
            .Where(c =>
                (userId != null && c.UserId == userId) ||
                (guestId != null && c.GuestId == guestId))
            .SelectMany(c => c.Items.Select(i => i.ProductId))
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = _context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .ApplyFilters(request)
            .ApplySorting(request.Sort, userId, guestId)
            .ProjectToType<ProductDto>();

        var paged = await query.ToPagedResultAsync(request.PageIndex, request.PageSize, cancellationToken);

        // Handle IsInCart here (not via Mapster)
        if (paged?.Items is { } items && cartProductIds.Count > 0)
        {
            foreach (var item in items)
                item.IsInCart = cartProductIds.Contains(item.Id);
        }

        return Result<PagedResult<ProductDto>>.Success(paged);
    }
}