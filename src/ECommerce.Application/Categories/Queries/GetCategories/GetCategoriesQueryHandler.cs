using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Categories.Queries.GetCategories;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<List<CategoryDto>>>
{
    private readonly ApplicationDbContext _context;

    public GetCategoriesQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var items = await _context.Categories
            .OrderBy(c => c.NameEn)
            .ProjectToType<CategoryDto>()
            .ToListAsync(cancellationToken);

        return Result<List<CategoryDto>>.Success(items);
    }
}

