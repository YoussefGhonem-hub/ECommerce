using ECommerce.Application.Common;
using ECommerce.Application.Products.Queries.GetProductById;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Reviews.Queries;

public record GetProductReviewsQuery(Guid ProductId) : IRequest<Result<List<ProductReviewDto>>>;

public class GetProductReviewsHandler : IRequestHandler<GetProductReviewsQuery, Result<List<ProductReviewDto>>>
{
    private readonly ApplicationDbContext _context;

    public GetProductReviewsHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ProductReviewDto>>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _context.ProductReviews
            .Where(r => r.ProductId == request.ProductId && r.IsApproved)
            .OrderByDescending(r => r.CreatedDate)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedDate = r.CreatedDate,
                UserFullName = r.User.FullName
            })
            .ToListAsync(cancellationToken);

        return Result<List<ProductReviewDto>>.Success(reviews);
    }
}