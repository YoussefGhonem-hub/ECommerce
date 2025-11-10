using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Orders.Commands;

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus Status) : IRequest<Result<bool>>;
public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public UpdateOrderStatusHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order is null) return Result<bool>.Failure("Order not found");

        order.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

