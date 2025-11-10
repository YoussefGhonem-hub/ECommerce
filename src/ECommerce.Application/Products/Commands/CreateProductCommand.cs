using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.Storage;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Commands;

public record CreateProductCommand(
    string NameAr,
    string NameEn,
    string? Description,
    string SKU,
    Guid CategoryId,
    decimal Price,
    int StockQuantity,
    bool AllowBackorder,
    string? Brand,
    IReadOnlyList<IFormFile>? Images,    // new
    int? MainImageIndex                  // new: index within Images to mark as main
) : IRequest<Result<Guid>>;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _files;

    public CreateProductHandler(ApplicationDbContext context, IFileStorage files)
    {
        _context = context;
        _files = files;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Ensure SKU uniqueness
        var skuExists = await _context.Products.AnyAsync(p => p.SKU == request.SKU, cancellationToken);
        if (skuExists) return Result<Guid>.Failure("SKU already exists.");

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
            Brand = request.Brand
        };

        // Save first so we have product.Id
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // Handle images
        var savedImages = new List<ProductImage>();
        if (request.Images is { Count: > 0 })
        {
            var idx = 0;
            foreach (var file in request.Images)
            {
                var relative = await _files.SaveAsync(file, "productimages", cancellationToken);
                savedImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    Path = relative,
                    IsMain = false,
                    SortOrder = idx++
                });
            }

            var mainIdx = request.MainImageIndex.GetValueOrDefault(0);
            if (mainIdx < 0 || mainIdx >= savedImages.Count) mainIdx = 0;
            savedImages[mainIdx].IsMain = true;

            _context.ProductImages.AddRange(savedImages);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<Guid>.Success(product.Id);
    }
}
