using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;

namespace ECommerce.Application.Categories.Commands;

public record CreateCategoryCommand(string NameAr, string NameEn, Guid? ParentId, bool IsFeatured) : IRequest<Result<Guid>>;
public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateCategoryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            NameAr = request.NameEn,
            NameEn = request.NameEn,
            ParentId = request.ParentId,
            IsFeatured = request.IsFeatured
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(category.Id);
    }
}