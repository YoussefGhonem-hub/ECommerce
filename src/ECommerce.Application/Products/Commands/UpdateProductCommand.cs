using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;

namespace ECommerce.Application.Products.Commands;

public record UpdateProductCommand(Guid Id, string NameAr, string NameEn, string? Description, string SKU, Guid CategoryId, decimal Price, int StockQuantity, bool AllowBackorder, string? Brand, string? ImageUrl) : IRequest<Result<bool>>;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public UpdateProductHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync(new object?[] { request.Id }, cancellationToken);
        if (product is null) return Result<bool>.Failure("Not found");
        product.NameAr = request.NameAr;
        product.NameEn = request.NameEn;
        product.Description = request.Description;
        product.SKU = request.SKU;
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.AllowBackorder = request.AllowBackorder;
        product.Brand = request.Brand;
        product.ImageUrl = request.ImageUrl;
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
