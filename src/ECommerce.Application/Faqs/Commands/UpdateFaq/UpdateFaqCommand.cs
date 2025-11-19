using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Faqs.Commands.UpdateFaq;

public record UpdateFaqCommand(
    Guid Id,
    Guid CategoryId,
    string QuestionEn,
    string QuestionAr,
    string AnswerEn,
    string AnswerAr,
    bool IsActive,
    int DisplayOrder
) : IRequest<Result<Guid>>;

public class UpdateFaqHandler : IRequestHandler<UpdateFaqCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;
    public UpdateFaqHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(UpdateFaqCommand request, CancellationToken cancellationToken)
    {
        var faq = await _context.Faqs.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        if (faq is null) return Result<Guid>.Failure("FAQ not found");

        var categoryExists = await _context.FaqCategories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists) return Result<Guid>.Failure("Category not found");

        faq.FaqCategoryId = request.CategoryId;
        faq.QuestionEn = request.QuestionEn.Trim();
        faq.QuestionAr = request.QuestionAr.Trim();
        faq.AnswerEn = request.AnswerEn.Trim();
        faq.AnswerAr = request.AnswerAr.Trim();
        faq.IsActive = request.IsActive;
        faq.DisplayOrder = request.DisplayOrder;

        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(faq.Id);
    }
}