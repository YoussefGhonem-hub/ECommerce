using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Orders.Queries.GetMyOrders;

public class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, Result<PagedResult<OrderDto>>>
{
    private readonly ApplicationDbContext _context;

    public GetMyOrdersQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<OrderDto>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.UserId == CurrentUser.Id)
            .OrderByDescending(o => o.CreatedDate);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Total = o.Total,
                Status = (int)o.Status,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product != null ? (i.Product.NameEn ?? i.Product.NameAr ?? "") : "",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var paged = PagedResult<OrderDto>.Create(items, total, request.PageNumber, request.PageSize);
        return Result<PagedResult<OrderDto>>.Success(paged);
    }
}
