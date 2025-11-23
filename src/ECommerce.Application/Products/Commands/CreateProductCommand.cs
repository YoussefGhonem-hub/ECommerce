using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.Storage;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Commands;

// Map existing attributes (by id) and existing allowed value ids to the new product.
// No creation of attributes or values.
public record ProductAttributeCreate(
    Guid AttributeId,
    IReadOnlyList<Guid>? ValueIds // existing ProductAttributeValue ids belonging to AttributeId
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
    IReadOnlyList<ProductAttributeCreate>? Attributes // NEW
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
        var validation = new Dictionary<string, string[]>();

        // SKU uniqueness
        if (await _context.Products.AnyAsync(p => p.SKU == request.SKU, cancellationToken))
        {
            Append(validation, nameof(request.SKU), "SKU already exists.");
        }

        // Validate attributes (only existence; we will map after product saved)
        var attributeIds = request.Attributes?
            .Select(a => a.AttributeId)
            .Distinct()
            .ToList() ?? new List<Guid>();

        var attributesById = attributeIds.Count == 0
            ? new Dictionary<Guid, ProductAttribute>()
            : await _context.ProductAttributes
                .Where(a => attributeIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken);

        // Collect intended mappings (attributeId -> distinct valueIds)
        var attributeToValueIds = new Dictionary<Guid, HashSet<Guid>>();

        if (request.Attributes is { Count: > 0 })
        {
            foreach (var attr in request.Attributes)
            {
                if (!attributesById.ContainsKey(attr.AttributeId))
                {
                    Append(validation, "Attributes", $"Attribute not found: {attr.AttributeId}");
                    continue;
                }

                if (attr.ValueIds is { Count: > 0 })
                {
                    var distinct = attr.ValueIds.Where(v => v != Guid.Empty).Distinct().ToList();
                    // Validate that each value belongs to the attribute
                    var validValueIds = await _context.ProductAttributeValues
                        .Where(v => v.ProductAttributeId == attr.AttributeId && distinct.Contains(v.Id))
                        .Select(v => v.Id)
                        .ToListAsync(cancellationToken);

                    var invalid = distinct.Except(validValueIds).ToList();
                    if (invalid.Count > 0)
                        Append(validation, "AttributeValues",
                            $"Values do not belong to attribute {attr.AttributeId}: {string.Join(",", invalid)}");

                    if (!attributeToValueIds.TryGetValue(attr.AttributeId, out var set))
                    {
                        set = new HashSet<Guid>();
                        attributeToValueIds[attr.AttributeId] = set;
                    }
                    foreach (var vid in validValueIds)
                        set.Add(vid);
                }
            }
        }

        if (validation.Count > 0)
            return Result<Guid>.Validation(validation);

        // Create product first
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
        await _context.SaveChangesAsync(cancellationToken); // assign product.Id

        // Map attributes & values
        if (attributeToValueIds.Count > 0)
        {
            var mappings = new List<ProductAttributeMapping>();
            foreach (var kvp in attributeToValueIds)
            {
                var attrId = kvp.Key;
                foreach (var valId in kvp.Value)
                {
                    mappings.Add(new ProductAttributeMapping
                    {
                        ProductId = product.Id,
                        ProductAttributeId = attrId,
                        ProductAttributeValueId = valId
                    });
                }
            }

            if (mappings.Count > 0)
            {
                _context.ProductAttributeMappings.AddRange(mappings);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // Images
        if (request.Images is { Count: > 0 })
        {
            var list = new List<ProductImage>();
            var idx = 0;
            foreach (var file in request.Images)
            {
                var relative = await _files.SaveAsync(file, "productimages", cancellationToken);
                list.Add(new ProductImage
                {
                    ProductId = product.Id,
                    Path = relative,
                    IsMain = false,
                    SortOrder = idx++
                });
            }

            var mainIdx = request.MainImageIndex.GetValueOrDefault(0);
            if (mainIdx < 0 || mainIdx >= list.Count) mainIdx = 0;
            list[mainIdx].IsMain = true;

            _context.ProductImages.AddRange(list);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result<Guid>.Success(product.Id);
    }

    private static void Append(Dictionary<string, string[]> bag, string key, string message)
    {
        if (bag.TryGetValue(key, out var arr))
            bag[key] = arr.Concat(new[] { message }).ToArray();
        else
            bag[key] = new[] { message };
    }
}