using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;

namespace ECommerce.Application.Categories.Commands;

public record DeleteCategoryCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public DeleteCategoryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync(new object?[] { request.Id }, cancellationToken);
        if (category is null) return Result<bool>.Failure("Not found");
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}