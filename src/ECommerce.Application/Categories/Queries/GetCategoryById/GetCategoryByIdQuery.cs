using ECommerce.Application.Categories.Queries.GetCategories;
using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Categories.Queries.GetCategoryById;

public record GetCategoryByIdQuery(Guid Id) : IRequest<Result<CategoryDto>>;

public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
    private readonly ApplicationDbContext _context;
    public GetCategoryByIdHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken ct)
    {
        var category = await _context.Categories
            .Where(c => c.Id == request.Id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                NameEn = c.NameEn,
                NameAr = c.NameAr,
                ParentId = c.ParentId,
                IsFeatured = c.IsFeatured
            })
            .FirstOrDefaultAsync(ct);

        return category is null
            ? Result<CategoryDto>.Failure("Category.NotFound")
            : Result<CategoryDto>.Success(category);
    }
}