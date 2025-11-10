using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using System.Data.Entity;

namespace ECommerce.Application.Reviews.Queries;

public record GetProductReviewsQuery(Guid ProductId) : IRequest<List<ProductReview>>;
public class GetProductReviewsHandler : IRequestHandler<GetProductReviewsQuery, List<ProductReview>>
{
    private readonly ApplicationDbContext _context;

    public GetProductReviewsHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductReview>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        return await _context.ProductReviews
            .Where(r => r.ProductId == request.ProductId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(cancellationToken);
    }
}
