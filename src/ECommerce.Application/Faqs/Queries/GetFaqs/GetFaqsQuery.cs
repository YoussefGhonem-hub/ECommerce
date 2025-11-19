using ECommerce.Application.Common;
using ECommerce.Application.Faqs.Dtos;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Faqs.Queries.GetFaqs;

// Returns grouped FAQs by Category (English & Arabic names)
public record GetFaqsQuery(
    bool OnlyActive = true,
    Guid? CategoryId = null
) : IRequest<Result<List<FaqCategoryGroupDto>>>;

public class GetFaqsHandler : IRequestHandler<GetFaqsQuery, Result<List<FaqCategoryGroupDto>>>
{
    private readonly ApplicationDbContext _context;
    public GetFaqsHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<List<FaqCategoryGroupDto>>> Handle(GetFaqsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Faqs
            .Include(f => f.Category)
            .AsQueryable();

        if (request.OnlyActive)
            query = query.Where(f => f.IsActive && f.Category!.IsActive);

        if (request.CategoryId.HasValue)
            query = query.Where(f => f.FaqCategoryId == request.CategoryId.Value);

        var grouped = await query
            .OrderBy(f => f.Category!.DisplayOrder)
            .ThenBy(f => f.DisplayOrder)
            .GroupBy(f => new
            {
                f.Category!.Id,
                f.Category.NameEn,
                f.Category.NameAr,
                f.Category.DisplayOrder
            })
            .Select(g => new FaqCategoryGroupDto
            {
                CategoryId = g.Key.Id,
                CategoryNameEn = g.Key.NameEn,
                CategoryNameAr = g.Key.NameAr,
                CategoryDisplayOrder = g.Key.DisplayOrder,
                Faqs = g.Select(f => new FaqItemDto
                {
                    Id = f.Id,
                    QuestionEn = f.QuestionEn,
                    QuestionAr = f.QuestionAr,
                    AnswerEn = f.AnswerEn,
                    AnswerAr = f.AnswerAr,
                    DisplayOrder = f.DisplayOrder,
                    IsActive = f.IsActive
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Result<List<FaqCategoryGroupDto>>.Success(grouped);
    }
}

