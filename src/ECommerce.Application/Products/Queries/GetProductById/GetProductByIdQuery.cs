using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<Product?>;
public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Product?>
{
    private readonly ApplicationDbContext _context;

    public GetProductByIdHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
    }
}
