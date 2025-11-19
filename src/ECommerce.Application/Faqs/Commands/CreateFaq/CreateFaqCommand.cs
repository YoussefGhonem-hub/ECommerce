using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Faqs.Commands.CreateFaq;

public record CreateFaqCommand(
    Guid CategoryId,
    string QuestionEn,
    string QuestionAr,
    string AnswerEn,
    string AnswerAr,
    bool IsActive,
    int DisplayOrder
) : IRequest<Result<Guid>>;

public class CreateFaqHandler : IRequestHandler<CreateFaqCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;
    public CreateFaqHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateFaqCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await _context.FaqCategories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists) return Result<Guid>.Failure("Category not found");

        var entity = new Faq
        {
            FaqCategoryId = request.CategoryId,
            QuestionEn = request.QuestionEn.Trim(),
            QuestionAr = request.QuestionAr.Trim(),
            AnswerEn = request.AnswerEn.Trim(),
            AnswerAr = request.AnswerAr.Trim(),
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };

        _context.Faqs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
    }
}