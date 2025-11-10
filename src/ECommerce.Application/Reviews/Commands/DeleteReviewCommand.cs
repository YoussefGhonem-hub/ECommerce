
using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Reviews.Commands;

public record DeleteReviewCommand(string UserId, Guid ReviewId) : IRequest<Result<bool>>;

public class DeleteReviewHandler : IRequestHandler<DeleteReviewCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public DeleteReviewHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _context.ProductReviews.FirstOrDefaultAsync(r => r.Id == request.ReviewId && r.UserId == request.UserId, cancellationToken);
        if (review is null) return Result<bool>.Success(true);
        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
