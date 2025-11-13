using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

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

        // persist the review first so the aggregation sees it
        await _context.SaveChangesAsync(cancellationToken);

        // recompute average from approved reviews (change filter if you want all reviews)
        var avg = await _context.ProductReviews
            .Where(r => r.ProductId == request.ProductId && r.IsApproved)
            .AverageAsync(r => (double?)r.Rating, cancellationToken) ?? 0.0;

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is not null)
        {
            product.AverageRating = Math.Round(avg, 2);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}