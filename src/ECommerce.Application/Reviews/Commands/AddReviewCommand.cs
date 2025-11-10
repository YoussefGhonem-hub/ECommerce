using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using System.Data.Entity;

namespace ECommerce.Application.Reviews.Commands;

public record AddReviewCommand(string UserId, Guid ProductId, int Rating, string? Comment) : IRequest<Result<bool>>;
public class AddReviewHandler : IRequestHandler<AddReviewCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public AddReviewHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(AddReviewCommand request, CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return Result<bool>.Failure("Rating must be between 1 and 5");

        // user can update own review for same product
        var existing = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.UserId == request.UserId && r.ProductId == request.ProductId, cancellationToken);

        if (existing is null)
        {
            _context.ProductReviews.Add(new ProductReview
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                Rating = request.Rating,
                Comment = request.Comment
            });
        }
        else
        {
            existing.Rating = request.Rating;
            existing.Comment = request.Comment;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
