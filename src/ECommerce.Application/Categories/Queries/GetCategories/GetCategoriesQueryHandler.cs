using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Categories.Queries.GetCategories;
public class GetCategoriesQuery : BaseFilterDto, IRequest<Result<PagedResult<CategoryDto>>>
{
    public string? Search { get; set; }
}

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<PagedResult<CategoryDto>>>
{
    private readonly ApplicationDbContext _context;
    public GetCategoriesQueryHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<PagedResult<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.NameEn.ToLower().Contains(term) ||
                c.NameAr.ToLower().Contains(term));
        }

        // Sorting
        if (string.IsNullOrWhiteSpace(request.Sort))
        {
            query = request.Descending
                ? query.OrderByDescending(c => c.NameEn)
                : query.OrderBy(c => c.NameEn);
        }
        else
        {
            // Only allow NameEn / NameAr / IsFeatured (fallback NameEn)
            var sort = request.Sort.Trim();
            IOrderedQueryable<Domain.Entities.Category> ordered;
            ordered = (sort.ToLower()) switch
            {
                "namear" => request.Descending
                    ? query.OrderByDescending(c => c.NameAr)
                    : query.OrderBy(c => c.NameAr),
                "isfeatured" => request.Descending
                    ? query.OrderByDescending(c => c.IsFeatured)
                    : query.OrderBy(c => c.IsFeatured),
                _ => request.Descending
                    ? query.OrderByDescending(c => c.NameEn)
                    : query.OrderBy(c => c.NameEn)
            };
            query = ordered;
        }

        // Projection before paging
        var projected = query.ProjectToType<CategoryDto>();

        var paged = await projected.ToPagedResultAsync(request.PageIndex, request.PageSize, cancellationToken);

        return Result<PagedResult<CategoryDto>>.Success(paged);
    }
}

