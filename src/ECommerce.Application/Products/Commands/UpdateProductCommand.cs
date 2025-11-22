using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.Storage;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Commands;

public record ProductAttributeUpdateSelection(
    Guid? AttributeId,
    string? NewAttributeName,
    List<Guid>? ValueIds,
    List<string>? NewValues,
    bool RemoveAttribute = false
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
    IReadOnlyList<ProductAttributeUpdateSelection>? Attributes
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

    public async Task<Result<bool>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var validation = new Dictionary<string, string[]>();

        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (product is null)
            return Result<bool>.Failure("Product.NotFound");

        // SKU uniqueness (allow same SKU if unchanged)
        if (!string.Equals(product.SKU, request.SKU, StringComparison.OrdinalIgnoreCase))
        {
            var skuExists = await _context.Products
                .AnyAsync(p => p.SKU == request.SKU && p.Id != product.Id, ct);
            if (skuExists)
                validation[nameof(request.SKU)] = new[] { "SKU already exists." };
        }

        if (validation.Count > 0)
            return Result<bool>.Validation(validation);

        using var tx = await _context.Database.BeginTransactionAsync(ct);

        // Basic scalar updates
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

        // --------------------- IMAGES (phase 1) ---------------------
        var originalMainImageId = product.Images.FirstOrDefault(i => i.IsMain)?.Id;
        var removingMain = originalMainImageId != null &&
                           request.RemoveImageIds is { Count: > 0 } &&
                           request.RemoveImageIds.Contains(originalMainImageId.Value);

        if (request.RemoveImageIds is { Count: > 0 })
        {
            var toRemove = product.Images.Where(i => request.RemoveImageIds.Contains(i.Id)).ToList();
            if (toRemove.Count > 0)
            {
                _context.ProductImages.RemoveRange(toRemove);
                await _files.DeleteManyAsync(toRemove.Select(i => i.Path), ct);
            }
        }

        var addedImages = new List<ProductImage>();
        if (request.NewImages is { Count: > 0 })
        {
            var startOrder = product.Images.Count > 0 ? product.Images.Max(i => i.SortOrder) + 1 : 0;
            var idx = 0;
            foreach (var file in request.NewImages)
            {
                var relative = await _files.SaveAsync(file, "productimages", ct);
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

        // Decide main image
        if (request.SetMainImageId.HasValue)
        {
            var target = product.Images.FirstOrDefault(i => i.Id == request.SetMainImageId.Value);
            if (target == null)
            {
                Append(validation, "Images", "MainImageId invalid.");
            }
            else
            {
                foreach (var img in product.Images) img.IsMain = img.Id == target.Id;
            }
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
                foreach (var img in product.Images) img.IsMain = false;
                var candidate = product.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
                if (candidate != null) candidate.IsMain = true;
            }
        }

        // Normalize to single main
        var mains = product.Images.Where(i => i.IsMain).ToList();
        if (mains.Count > 1)
        {
            var keep = mains.OrderBy(i => i.SortOrder).First();
            foreach (var img in product.Images)
                img.IsMain = img.Id == keep.Id;
        }

        // --------------------- ATTRIBUTES & VALUES (phase 1) ---------------------
        var mappingIntents = new List<MappingIntent>();

        if (request.Attributes is { Count: > 0 })
        {
            var attrCacheByName = new Dictionary<string, ProductAttribute>(StringComparer.OrdinalIgnoreCase);

            // Load existing mappings for later diff operations (only once now)
            var existingMappings = await _context.ProductAttributeMappings
                .Where(m => m.ProductId == product.Id)
                .ToListAsync(ct);

            foreach (var sel in request.Attributes)
            {
                // Removal path
                if (sel.RemoveAttribute)
                {
                    // Resolve attribute (id or name)
                    ProductAttribute? toRemoveAttr = null;
                    if (sel.AttributeId.HasValue)
                        toRemoveAttr = await _context.ProductAttributes.FirstOrDefaultAsync(a => a.Id == sel.AttributeId.Value, ct);
                    else if (!string.IsNullOrWhiteSpace(sel.NewAttributeName))
                    {
                        var name = sel.NewAttributeName.Trim();
                        toRemoveAttr = await _context.ProductAttributes.FirstOrDefaultAsync(a => a.Name.ToLower() == name.ToLower(), ct);
                    }

                    if (toRemoveAttr is null)
                    {
                        Append(validation, "Attributes", "RemoveAttribute target not found.");
                        continue;
                    }

                    var toRemoveMappings = existingMappings.Where(m => m.ProductAttributeId == toRemoveAttr.Id).ToList();
                    if (toRemoveMappings.Count > 0)
                    {
                        _context.ProductAttributeMappings.RemoveRange(toRemoveMappings);
                        existingMappings.RemoveAll(m => m.ProductAttributeId == toRemoveAttr.Id);
                    }
                    // No mapping intent needed for pure removal
                    continue;
                }

                // Resolve attribute (like create logic)
                var attribute = await ResolveAttributeAsync(sel.AttributeId, sel.NewAttributeName, attrCacheByName, ct);
                if (attribute is null)
                {
                    Append(validation, "Attributes", "Attribute resolution failed.");
                    continue;
                }

                // Existing selected values
                var existingValueIds = new List<Guid>();
                if (sel.ValueIds is { Count: > 0 })
                {
                    // validate membership
                    var validIds = await _context.ProductAttributeValues
                        .Where(v => v.ProductAttributeId == attribute.Id && sel.ValueIds.Contains(v.Id))
                        .Select(v => v.Id)
                        .ToListAsync(ct);

                    if (validIds.Count != sel.ValueIds.Count)
                    {
                        Append(validation, "AttributeValues", $"One or more value IDs invalid for attribute {attribute.Id}.");
                        continue;
                    }
                    existingValueIds.AddRange(validIds);
                }
                // New textual values
                var newValueTexts = Enumerable.Empty<string>()
                    .Concat(sel.NewValues)
                    .Select(v => v?.Trim())
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Create missing values (avoid duplicates)
                foreach (var val in newValueTexts)
                {
                    var tracked = _context.ChangeTracker.Entries<ProductAttributeValue>()
                        .Select(e => e.Entity)
                        .Any(e => e.ProductAttributeId == attribute.Id &&
                                  e.Value.ToLower() == val.ToLower());
                    if (tracked) continue;

                    var dbExists = await _context.ProductAttributeValues
                        .AnyAsync(v => v.ProductAttributeId == attribute.Id &&
                                       v.Value.ToLower() == val.ToLower(), ct);
                    if (!dbExists && val != null && val != "null")
                    {
                        _context.ProductAttributeValues.Add(new ProductAttributeValue
                        {
                            ProductAttributeId = attribute.Id,
                            Value = val
                        });
                    }
                }

                var valuesSpecified = existingValueIds.Count > 0 || newValueTexts.Count > 0;
                mappingIntents.Add(new MappingIntent(attribute.Id, existingValueIds, newValueTexts, valuesSpecified));
            }

            // Persist newly created attributes & values
            if (validation.Count == 0)
                await _context.SaveChangesAsync(ct);

            // --------------------- MAPPINGS (phase 2) ---------------------
            if (validation.Count == 0 && mappingIntents.Count > 0)
            {
                foreach (var intent in mappingIntents)
                {
                    // Resolve IDs for new texts
                    var textIds = intent.NewValueTexts.Count == 0
                        ? new List<Guid>()
                        : await _context.ProductAttributeValues
                            .Where(v => v.ProductAttributeId == intent.AttributeId &&
                                        intent.NewValueTexts.Contains(v.Value))
                            .Select(v => v.Id)
                            .ToListAsync(ct);

                    var finalValueIds = intent.ExistingValueIds.Concat(textIds)
                        .Distinct()
                        .ToList();

                    // Current mappings for this attribute
                    var current = await _context.ProductAttributeMappings
                        .Where(m => m.ProductId == product.Id && m.ProductAttributeId == intent.AttributeId)
                        .ToListAsync(ct);

                    if (finalValueIds.Count > 0)
                    {
                        // Remove null mappings + mappings not in final set
                        var remove = current
                            .Where(m => m.ProductAttributeValueId == null ||
                                        !finalValueIds.Contains(m.ProductAttributeValueId!.Value))
                            .ToList();
                        if (remove.Count > 0) _context.ProductAttributeMappings.RemoveRange(remove);

                        var existingValueIdSet = current
                            .Where(m => m.ProductAttributeValueId != null)
                            .Select(m => m.ProductAttributeValueId!.Value)
                            .ToHashSet();

                        foreach (var vid in finalValueIds)
                        {
                            if (!existingValueIdSet.Contains(vid))
                            {
                                _context.ProductAttributeMappings.Add(new ProductAttributeMapping
                                {
                                    ProductId = product.Id,
                                    ProductAttributeId = intent.AttributeId,
                                    ProductAttributeValueId = vid
                                });
                            }
                        }
                    }
                    else
                    {
                        // Only add null mapping if user didn't specify ANY values intentionally
                        if (!intent.ValuesSpecified)
                        {
                            var hasNull = current.Any(m => m.ProductAttributeValueId == null);
                            if (!hasNull)
                            {
                                // Remove value mappings first
                                if (current.Count > 0)
                                {
                                    _context.ProductAttributeMappings.RemoveRange(
                                        current.Where(m => m.ProductAttributeValueId != null));
                                }

                                _context.ProductAttributeMappings.Add(new ProductAttributeMapping
                                {
                                    ProductId = product.Id,
                                    ProductAttributeId = intent.AttributeId,
                                    ProductAttributeValueId = null
                                });
                            }
                        }
                        else
                        {
                            // User intended value mapping but ended with none => treat as remove all
                            if (current.Count > 0)
                                _context.ProductAttributeMappings.RemoveRange(current);
                        }
                    }
                }

                await _context.SaveChangesAsync(ct);
            }
        }

        // Persist image changes & scalar updates if not already saved through attribute flow
        if (validation.Count == 0)
        {
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Result<bool>.Success(true);
        }

        await tx.RollbackAsync(ct);
        return Result<bool>.Validation(validation);
    }

    private async Task<ProductAttribute?> ResolveAttributeAsync(
        Guid? attributeId,
        string? newAttributeName,
        Dictionary<string, ProductAttribute> cache,
        CancellationToken ct)
    {
        if (attributeId.HasValue)
            return await _context.ProductAttributes.FirstOrDefaultAsync(a => a.Id == attributeId.Value, ct);

        if (!string.IsNullOrWhiteSpace(newAttributeName))
        {
            var name = newAttributeName.Trim();
            if (cache.TryGetValue(name, out var cached))
                return cached;

            var tracked = _context.ChangeTracker.Entries<ProductAttribute>()
                .Select(e => e.Entity)
                .FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (tracked != null)
            {
                cache[name] = tracked;
                return tracked;
            }

            var existing = await _context.ProductAttributes
                .FirstOrDefaultAsync(a => a.Name.ToLower() == name.ToLower(), ct);
            if (existing != null)
            {
                cache[name] = existing;
                return existing;
            }

            var attr = new ProductAttribute { Name = name };
            _context.ProductAttributes.Add(attr);
            cache[name] = attr;
            return attr;
        }

        return null;
    }

    private static void Append(Dictionary<string, string[]> bag, string key, string message)
    {
        if (bag.TryGetValue(key, out var arr))
            bag[key] = arr.Concat(new[] { message }).ToArray();
        else
            bag[key] = new[] { message };
    }

    private sealed class MappingIntent
    {
        public MappingIntent(Guid attributeId, List<Guid> existingValueIds, List<string> newValueTexts, bool valuesSpecified)
        {
            AttributeId = attributeId;
            ExistingValueIds = existingValueIds;
            NewValueTexts = newValueTexts;
            ValuesSpecified = valuesSpecified;
        }
        public Guid AttributeId { get; }
        public List<Guid> ExistingValueIds { get; }
        public List<string> NewValueTexts { get; }
        public bool ValuesSpecified { get; }
    }
}