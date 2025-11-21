using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Queries.GetProductByIdForUpdate;

public record GetProductByIdForUpdateQuery(Guid Id) : IRequest<Result<ProductForUpdateDto>>;

public class GetProductByIdForUpdateHandler : IRequestHandler<GetProductByIdForUpdateQuery, Result<ProductForUpdateDto>>
{
    private readonly ApplicationDbContext _context;
    public GetProductByIdForUpdateHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<ProductForUpdateDto>> Handle(GetProductByIdForUpdateQuery request, CancellationToken cancellationToken)
    {
        var dto = await _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Where(p => p.Id == request.Id)
            .ProjectToType<ProductForUpdateDto>()
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result<ProductForUpdateDto>.Failure("Product.NotFound");

        return Result<ProductForUpdateDto>.Success(dto);
    }
}