using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.Storage;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Commands;

public record UpdateProductCommand(
    Guid Id,
    string NameAr,
    string NameEn,
    string? Description,
    string SKU,
    Guid CategoryId,
    decimal Price,
    int StockQuantity,
    bool AllowBackorder,
    string? Brand,
    IReadOnlyList<IFormFile>? NewImages,    // new: images to add
    int? MainNewImageIndex,                 // new: if setting a new uploaded image as main
    IReadOnlyList<Guid>? RemoveImageIds,    // new: existing images to delete
    Guid? SetMainImageId                    // new: make an existing image main
) : IRequest<Result<bool>>;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _files;

    public UpdateProductHandler(ApplicationDbContext context, IFileStorage files)
    {
        _context = context;
        _files = files;
    }

    public async Task<Result<bool>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null) return Result<bool>.Failure("Not found");

        // Update fields
        product.NameAr = request.NameAr;
        product.NameEn = request.NameEn;
        product.Description = request.Description;
        product.SKU = request.SKU;
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.AllowBackorder = request.AllowBackorder;
        product.Brand = request.Brand;

        // Remove images
        if (request.RemoveImageIds is { Count: > 0 })
        {
            var toRemove = product.Images.Where(i => request.RemoveImageIds.Contains(i.Id)).ToList();
            if (toRemove.Count > 0)
            {
                _context.ProductImages.RemoveRange(toRemove);
                await _files.DeleteManyAsync(toRemove.Select(i => i.Path), cancellationToken);
            }
        }

        // Add new images
        var addedImages = new List<Domain.Entities.ProductImage>();
        if (request.NewImages is { Count: > 0 })
        {
            var startOrder = product.Images.Count > 0 ? product.Images.Max(i => i.SortOrder) + 1 : 0;
            var idx = 0;
            foreach (var file in request.NewImages)
            {
                var relative = await _files.SaveAsync(file, "productimages", cancellationToken);
                var img = new Domain.Entities.ProductImage
                {
                    ProductId = product.Id,
                    Path = relative,
                    IsMain = false,
                    SortOrder = startOrder + (idx++)
                };
                addedImages.Add(img);
                _context.ProductImages.Add(img);
            }
        }

        // Set main image if requested
        if (request.SetMainImageId.HasValue)
        {
            foreach (var img in product.Images)
                img.IsMain = img.Id == request.SetMainImageId.Value;
        }
        else if (request.MainNewImageIndex.HasValue && addedImages.Count > 0)
        {
            // Set one of the newly added images as main
            var mainIdx = request.MainNewImageIndex.Value;
            if (mainIdx < 0 || mainIdx >= addedImages.Count) mainIdx = 0;

            foreach (var img in product.Images) img.IsMain = false;
            addedImages[mainIdx].IsMain = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
