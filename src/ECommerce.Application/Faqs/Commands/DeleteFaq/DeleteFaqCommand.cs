using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Faqs.Commands.DeleteFaq;

public record DeleteFaqCommand(Guid Id) : IRequest<Result<Guid>>;

public class DeleteFaqHandler : IRequestHandler<DeleteFaqCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;
    public DeleteFaqHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(DeleteFaqCommand request, CancellationToken cancellationToken)
    {
        var faq = await _context.Faqs.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        if (faq is null) return Result<Guid>.Failure("FAQ not found");

        _context.Faqs.Remove(faq);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(request.Id);
    }
}