using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Commands;

public record DeleteProductCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _files;

    public DeleteProductHandler(ApplicationDbContext context, IFileStorage files)
    {
        _context = context;
        _files = files;
    }

    public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null) return Result<bool>.Failure("Not found");

        // Delete image files
        if (product.Images.Count > 0)
        {
            await _files.DeleteManyAsync(product.Images.Select(i => i.Path), cancellationToken);
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}