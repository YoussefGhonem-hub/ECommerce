using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.Storage;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Commands;

public record ProductAttributeSelection(
    Guid? AttributeId,
    string? NewAttributeName,
    Guid? ValueId,
    string? NewValue,
    List<string>? NewValues
);

public record CreateProductCommand(
    string NameAr,
    string NameEn,
    string? DescriptionEn,
    string? DescriptionAr,
    string SKU,
    Guid CategoryId,
    decimal Price,
    int StockQuantity,
    bool AllowBackorder,
    string? Brand,
    IReadOnlyList<IFormFile>? Images,
    int? MainImageIndex,
    IReadOnlyList<ProductAttributeSelection>? Attributes
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

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var validation = new Dictionary<string, string[]>();

        // SKU uniqueness
        if (await _context.Products.AnyAsync(p => p.SKU == request.SKU, ct))
            validation[nameof(request.SKU)] = new[] { "SKU already exists." };

        if (validation.Count > 0)
            return Result<Guid>.Validation(validation);

        using var tx = await _context.Database.BeginTransactionAsync(ct);

        // 1) Create product first
        var product = new Product
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            DescriptionAr = request.DescriptionAr,
            DescriptionEn = request.DescriptionEn,
            SKU = request.SKU,
            CategoryId = request.CategoryId,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            AllowBackorder = request.AllowBackorder,
            Brand = request.Brand
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);

        // Intents to build mappings AFTER we persist attributes/values
        var mappingIntents = new List<MappingIntent>();

        // 2) Ensure attributes and values exist (but DO NOT map yet)
        if (request.Attributes is { Count: > 0 })
        {
            // cache to avoid duplicate attribute creation within the same request
            var attrCacheByName = new Dictionary<string, ProductAttribute>(StringComparer.OrdinalIgnoreCase);

            foreach (var sel in request.Attributes)
            {
                if (sel.AttributeId is null && string.IsNullOrWhiteSpace(sel.NewAttributeName))
                    continue;

                // Resolve attribute
                var attribute = await ResolveAttributeAsync(sel, attrCacheByName, ct);
                if (attribute is null)
                {
                    Append(validation, "Attributes", $"Attribute not found: {sel.AttributeId}");
                    continue;
                }

                // Collect intended existing value IDs (validate they belong to attribute)
                var existingValueIds = new List<Guid>();
                if (sel.ValueId.HasValue)
                {
                    var ok = await _context.ProductAttributeValues
                        .AnyAsync(v => v.Id == sel.ValueId.Value && v.ProductAttributeId == attribute.Id, ct);
                    if (!ok)
                    {
                        Append(validation, "AttributeValues", $"Value {sel.ValueId.Value} does not belong to attribute {attribute.Id}");
                        continue;
                    }
                    existingValueIds.Add(sel.ValueId.Value);
                }

                // Collect textual new values (single + multiple) - create if not exists
                var newValueTexts = Enumerable.Empty<string>()
                    // Single new value
                    .Concat(string.IsNullOrWhiteSpace(sel.NewValue) ? Array.Empty<string>() : new[] { sel.NewValue })
                    // Multiple new values (could include nulls or blanks from client)
                    .Concat(sel.NewValues)
                    // Normalize (still keep nulls as null for filtering)
                    .Select(v => v?.Trim())
                    // Remove null/empty
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    // Deduplicate case-insensitive
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Create missing ProductAttributeValue rows (avoid duplicates by checking tracked + DB)
                foreach (var val in newValueTexts)
                {
                    // Check already tracked (unsaved new value)
                    var tracked = _context.ChangeTracker.Entries<ProductAttributeValue>()
                        .Any(e => e.Entity.ProductAttributeId == attribute.Id &&
                                  e.Entity.Value.Equals(val, StringComparison.OrdinalIgnoreCase));

                    if (tracked) continue;

                    // Check existing in DB
                    var dbExists = await _context.ProductAttributeValues
                        .AnyAsync(v => v.ProductAttributeId == attribute.Id &&
                                       v.Value == val, ct);

                    if (!dbExists && val != "null")
                    {
                        _context.ProductAttributeValues.Add(new ProductAttributeValue
                        {
                            ProductAttributeId = attribute.Id,
                            Value = val
                        });
                    }
                }

                // record mapping intent (valuesSpecified tells us if user intended value mapping)
                var valuesSpecified = existingValueIds.Count > 0 || newValueTexts.Count > 0;
                mappingIntents.Add(new MappingIntent(attribute.Id, existingValueIds, newValueTexts, valuesSpecified));
            }

            // Persist attributes + values so IDs exist for mapping
            await _context.SaveChangesAsync(ct);

            // 3) Build mappings strictly AFTER SaveChanges so we have real IDs
            foreach (var intent in mappingIntents)
            {
                // Resolve IDs for textual values now in DB
                var textIds = intent.NewValueTexts.Count == 0
                    ? new List<Guid>()
                    : await _context.ProductAttributeValues
                        .Where(v => v.ProductAttributeId == intent.AttributeId &&
                                    intent.NewValueTexts.Contains(v.Value))
                        .Select(v => v.Id)
                        .ToListAsync(ct);

                var finalValueIds = intent.ExistingValueIds.Concat(textIds).Distinct().ToList();

                if (finalValueIds.Count > 0)
                {
                    // add mapping per value id (skip duplicates)
                    var existing = await _context.ProductAttributeMappings
                        .Where(m => m.ProductId == product.Id &&
                                    m.ProductAttributeId == intent.AttributeId &&
                                    m.ProductAttributeValueId != null)
                        .Select(m => m.ProductAttributeValueId!.Value)
                        .ToListAsync(ct);
                    var existingSet = existing.ToHashSet();

                    foreach (var vid in finalValueIds)
                    {
                        if (!existingSet.Contains(vid))
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
                    // Only add null mapping if user did NOT specify any values at all
                    if (!intent.ValuesSpecified)
                    {
                        var existsNull = await _context.ProductAttributeMappings.AnyAsync(m =>
                            m.ProductId == product.Id &&
                            m.ProductAttributeId == intent.AttributeId &&
                            m.ProductAttributeValueId == null, ct);

                        if (!existsNull)
                        {
                            _context.ProductAttributeMappings.Add(new ProductAttributeMapping
                            {
                                ProductId = product.Id,
                                ProductAttributeId = intent.AttributeId,
                                ProductAttributeValueId = null
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(ct); // second SaveChanges for mappings
        }

        // 4) Images
        if (request.Images is { Count: > 0 })
            await SaveImagesAsync(product.Id, request.Images, request.MainImageIndex, ct);

        if (validation.Count > 0)
        {
            await tx.RollbackAsync(ct);
            return Result<Guid>.Validation(validation);
        }

        await tx.CommitAsync(ct);
        return Result<Guid>.Success(product.Id);
    }

    private async Task<ProductAttribute?> ResolveAttributeAsync(
        ProductAttributeSelection sel,
        Dictionary<string, ProductAttribute> cache,
        CancellationToken ct)
    {
        if (sel.AttributeId.HasValue)
        {
            return await _context.ProductAttributes
                .FirstOrDefaultAsync(a => a.Id == sel.AttributeId.Value, ct);
        }

        if (!string.IsNullOrWhiteSpace(sel.NewAttributeName))
        {
            var name = sel.NewAttributeName.Trim();

            if (cache.TryGetValue(name, out var cached))
                return cached;

            // tracked?
            var tracked = _context.ChangeTracker.Entries<ProductAttribute>()
                .Select(e => e.Entity)
                .FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (tracked != null)
            {
                cache[name] = tracked;
                return tracked;
            }

            // db?
            var existing = await _context.ProductAttributes
                .FirstOrDefaultAsync(a => a.Name.ToLower() == name.ToLower(), ct);
            if (existing != null)
            {
                cache[name] = existing;
                return existing;
            }

            // create new (ID available before save as GUID)
            var attr = new ProductAttribute { Name = name };
            _context.ProductAttributes.Add(attr);
            cache[name] = attr;
            return attr;
        }

        return null;
    }

    private async Task SaveImagesAsync(Guid productId, IReadOnlyList<IFormFile> images, int? mainImageIndex, CancellationToken ct)
    {
        var list = new List<ProductImage>();
        var idx = 0;
        foreach (var file in images)
        {
            var relative = await _files.SaveAsync(file, "productimages", ct);
            list.Add(new ProductImage
            {
                ProductId = productId,
                Path = relative,
                IsMain = false,
                SortOrder = idx++
            });
        }

        var mainIdx = mainImageIndex.GetValueOrDefault(0);
        if (mainIdx < 0 || mainIdx >= list.Count) mainIdx = 0;
        list[mainIdx].IsMain = true;

        _context.ProductImages.AddRange(list);
        await _context.SaveChangesAsync(ct);
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