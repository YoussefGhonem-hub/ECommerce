using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;

namespace ECommerce.Application.Products.Commands;

public record DeleteProductCommand(Guid Id) : IRequest<Result<bool>>;
public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public DeleteProductHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync(new object?[] { request.Id }, cancellationToken);
        if (product is null) return Result<bool>.Failure("Not found");
        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}