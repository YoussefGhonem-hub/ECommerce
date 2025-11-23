using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.Storage;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Commands;
public record ProductAttributeUpdate(
    Guid AttributeId,                  // MUST be an existing attribute
    IReadOnlyList<Guid>? AddValueIds,  // map these existing value IDs
    IReadOnlyList<Guid>? RemoveValueIds, // un-map these value IDs
    bool RemoveAttribute               // remove ALL mappings for this attribute
);

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
    Guid? SetMainImageId,
    IReadOnlyList<ProductAttributeUpdate>? Attributes
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

        var validation = new Dictionary<string, string[]>();

        // Basic updates
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

        // Images handling
        var originalMainImageId = product.Images.FirstOrDefault(i => i.IsMain)?.Id;
        var removingMain = originalMainImageId != null
                           && request.RemoveImageIds is { Count: > 0 }
                           && request.RemoveImageIds.Contains(originalMainImageId.Value);

        if (request.RemoveImageIds is { Count: > 0 })
        {
            var toRemove = product.Images.Where(i => request.RemoveImageIds.Contains(i.Id)).ToList();
            if (toRemove.Count > 0)
            {
                _context.ProductImages.RemoveRange(toRemove);
                await _files.DeleteManyAsync(toRemove.Select(i => i.Path), cancellationToken);
            }
        }

        var addedImages = new List<ProductImage>();
        if (request.NewImages is { Count: > 0 })
        {
            var startOrder = product.Images.Count > 0 ? product.Images.Max(i => i.SortOrder) + 1 : 0;
            var idx = 0;
            foreach (var file in request.NewImages)
            {
                var relative = await _files.SaveAsync(file, "productimages", cancellationToken);
                var img = new ProductImage
                {
                    ProductId = product.Id,
                    Path = relative,
                    IsMain = false,
                    SortOrder = startOrder + (idx++)
                };
                addedImages.Add(img);
                product.Images.Add(img);
                _context.ProductImages.Add(img);
            }
        }

        if (request.SetMainImageId.HasValue)
        {
            var targetId = request.SetMainImageId.Value;
            var target = product.Images.FirstOrDefault(i => i.Id == targetId);
            if (target == null)
                return Result<bool>.Failure("MainImage.InvalidId");
            foreach (var img in product.Images)
                img.IsMain = img.Id == targetId;
        }
        else if (request.MainNewImageIndex.HasValue && addedImages.Count > 0)
        {
            var mainIdx = request.MainNewImageIndex.Value;
            if (mainIdx < 0 || mainIdx >= addedImages.Count) mainIdx = 0;
            foreach (var img in product.Images) img.IsMain = false;
            addedImages[mainIdx].IsMain = true;
        }
        else
        {
            var hasMain = product.Images.Any(i => i.IsMain);
            if (!hasMain || removingMain)
            {
                var candidate = product.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
                foreach (var img in product.Images) img.IsMain = false;
                if (candidate != null) candidate.IsMain = true;
            }
        }

        var mains = product.Images.Where(i => i.IsMain).ToList();
        if (mains.Count > 1)
        {
            var keep = mains.OrderBy(i => i.SortOrder).First();
            foreach (var img in product.Images) img.IsMain = img.Id == keep.Id;
        }

        // Attribute mappings
        if (request.Attributes is { Count: > 0 })
        {
            var attributeIds = request.Attributes.Select(a => a.AttributeId).Distinct().ToList();
            var existingAttributes = await _context.ProductAttributes
                .Where(a => attributeIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken);

            var existingMappings = await _context.ProductAttributeMappings
                .Where(m => m.ProductId == product.Id)
                .ToListAsync(cancellationToken);

            foreach (var update in request.Attributes)
            {
                if (!existingAttributes.TryGetValue(update.AttributeId, out var attribute))
                {
                    Append(validation, "Attributes", $"Attribute not found: {update.AttributeId}");
                    continue;
                }

                // Remove entire attribute mapping set
                if (update.RemoveAttribute)
                {
                    var toRemove = existingMappings
                        .Where(m => m.ProductAttributeId == attribute.Id)
                        .ToList();
                    if (toRemove.Count > 0)
                    {
                        _context.ProductAttributeMappings.RemoveRange(toRemove);
                        foreach (var m in toRemove) existingMappings.Remove(m);
                    }
                    continue;
                }

                // Remove specific value mappings
                if (update.RemoveValueIds is { Count: > 0 })
                {
                    var toRemove = existingMappings
                        .Where(m => m.ProductAttributeId == attribute.Id
                                    && m.ProductAttributeValueId.HasValue
                                    && update.RemoveValueIds.Contains(m.ProductAttributeValueId.Value))
                        .ToList();
                    if (toRemove.Count > 0)
                    {
                        _context.ProductAttributeMappings.RemoveRange(toRemove);
                        foreach (var m in toRemove) existingMappings.Remove(m);
                    }
                }

                // Add mappings for existing value IDs
                if (update.AddValueIds is { Count: > 0 })
                {
                    var candidateIds = update.AddValueIds.Distinct().ToList();
                    var validIds = await _context.ProductAttributeValues
                        .Where(v => v.ProductAttributeId == attribute.Id && candidateIds.Contains(v.Id))
                        .Select(v => v.Id)
                        .ToListAsync(cancellationToken);

                    var invalid = candidateIds.Except(validIds).ToList();
                    if (invalid.Count > 0)
                        Append(validation, "AttributeValues", $"Values not found for attribute {attribute.Id}: {string.Join(",", invalid)}");

                    var existingValueMappingIds = existingMappings
                        .Where(m => m.ProductAttributeId == attribute.Id && m.ProductAttributeValueId != null)
                        .Select(m => m.ProductAttributeValueId!.Value)
                        .ToHashSet();

                    foreach (var vid in validIds)
                    {
                        if (!existingValueMappingIds.Contains(vid))
                        {
                            var mapping = new ProductAttributeMapping
                            {
                                ProductId = product.Id,
                                ProductAttributeId = attribute.Id,
                                ProductAttributeValueId = vid
                            };
                            _context.ProductAttributeMappings.Add(mapping);
                            existingMappings.Add(mapping);
                        }
                    }
                }
            }
        }

        if (validation.Count > 0)
            return Result<bool>.Validation(validation);

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static void Append(Dictionary<string, string[]> bag, string key, string message)
    {
        if (bag.TryGetValue(key, out var arr))
            bag[key] = arr.Concat(new[] { message }).ToArray();
        else
            bag[key] = new[] { message };
    }
}