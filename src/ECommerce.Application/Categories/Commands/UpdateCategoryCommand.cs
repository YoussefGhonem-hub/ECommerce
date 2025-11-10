using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;

namespace ECommerce.Application.Categories.Commands;

public record UpdateCategoryCommand(Guid Id, string NameEn, string NameAr, Guid? ParentId, bool IsFeatured) : IRequest<Result<bool>>;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public UpdateCategoryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync(new object?[] { request.Id }, cancellationToken);
        if (category is null) return Result<bool>.Failure("Not found");
        category.NameAr = request.NameAr;
        category.NameEn = request.NameEn;
        category.ParentId = request.ParentId;
        category.IsFeatured = request.IsFeatured;
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
