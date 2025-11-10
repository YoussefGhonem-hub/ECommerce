using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Orders.Queries.GetMyOrderById;

public record GetMyOrderByIdQuery(string UserId, Guid OrderId) : IRequest<Order?>;

public class GetMyOrderByIdHandler : IRequestHandler<GetMyOrderByIdQuery, Order?>
{
    private readonly ApplicationDbContext _context;

    public GetMyOrderByIdHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> Handle(GetMyOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == CurrentUser.Id, cancellationToken);
    }
}
