using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;

namespace ECommerce.Application.Products.Commands;

public record CreateProductCommand(string NameAr, string NameEn, string? Description, string SKU, Guid CategoryId, decimal Price, int StockQuantity, bool AllowBackorder, string? Brand, string? ImageUrl) : IRequest<Result<Guid>>;
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateProductHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Description = request.Description,
            SKU = request.SKU,
            CategoryId = request.CategoryId,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            AllowBackorder = request.AllowBackorder,
            Brand = request.Brand,
            ImageUrl = request.ImageUrl
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(product.Id);
    }
}
