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
    string? DescriptionAr,
    string? DescriptionEn,
    string SKU,
    Guid CategoryId,
    decimal Price,
    int StockQuantity,
    bool AllowBackorder,
    string? Brand,
    IReadOnlyList<IFormFile>? NewImages,
    int? MainNewImageIndex,
    IReadOnlyList<Guid>? RemoveImageIds,
    Guid? SetMainImageId
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

        if (product is null) return Result<bool>.Failure("Product.NotFound");

        // Update basic scalar fields
        product.NameAr = request.NameAr;
        product.NameEn = request.NameEn;
        product.DescriptionAr = request.DescriptionAr;
        product.DescriptionEn = request.DescriptionEn;
        product.SKU = request.SKU;
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.AllowBackorder = request.AllowBackorder;
        product.Brand = request.Brand;

        // Track if original main image will be removed
        var originalMainImageId = product.Images.FirstOrDefault(i => i.IsMain)?.Id;
        var removingMain = originalMainImageId != null
                           && request.RemoveImageIds is { Count: > 0 }
                           && request.RemoveImageIds.Contains(originalMainImageId.Value);

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

        // Add new images (all start as non-main)
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
                product.Images.Add(img); // ensure part of navigation collection for later main assignment
                _context.ProductImages.Add(img);
            }
        }

        // Decide new main image
        // Precedence: SetMainImageId (existing or newly added) > MainNewImageIndex (new uploads) > auto-fix (if no main)
        if (request.SetMainImageId.HasValue)
        {
            var targetId = request.SetMainImageId.Value;
            var target = product.Images.FirstOrDefault(i => i.Id == targetId);

            // Validate the target exists and is not removed
            if (target == null)
                return Result<bool>.Failure("MainImage.InvalidId");

            foreach (var img in product.Images)
                img.IsMain = img.Id == targetId;
        }
        else if (request.MainNewImageIndex.HasValue && addedImages.Count > 0)
        {
            var mainIdx = request.MainNewImageIndex.Value;
            if (mainIdx < 0 || mainIdx >= addedImages.Count) mainIdx = 0;

            foreach (var img in product.Images)
                img.IsMain = false;

            addedImages[mainIdx].IsMain = true;
        }
        else
        {
            // Automatic fix if:
            // - main removed OR
            // - product has no main at all after modifications
            var hasMain = product.Images.Any(i => i.IsMain);
            if (!hasMain || removingMain)
            {
                var candidate = product.Images
                    .OrderBy(i => i.SortOrder)
                    .FirstOrDefault();

                foreach (var img in product.Images)
                    img.IsMain = false;

                if (candidate != null)
                    candidate.IsMain = true;
            }
        }

        // Defensive: ensure only one main (in case of inconsistent state)
        var mains = product.Images.Where(i => i.IsMain).ToList();
        if (mains.Count > 1)
        {
            // Keep the one with smallest SortOrder as main
            var keep = mains.OrderBy(i => i.SortOrder).First();
            foreach (var img in product.Images)
                img.IsMain = img.Id == keep.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}