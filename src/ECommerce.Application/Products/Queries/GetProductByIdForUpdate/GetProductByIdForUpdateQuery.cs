using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Queries.GetProductByIdForUpdate;

public record GetProductByIdForUpdateQuery(Guid Id) : IRequest<Result<ProductForUpdateDto>>;

public class GetProductByIdForUpdateHandler : IRequestHandler<GetProductByIdForUpdateQuery, Result<ProductForUpdateDto>>
{
    private readonly ApplicationDbContext _context;
    public GetProductByIdForUpdateHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<ProductForUpdateDto>> Handle(GetProductByIdForUpdateQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
            return Result<ProductForUpdateDto>.Failure("Product.NotFound");

        // Load mappings (attribute + value)
        var mappings = await _context.ProductAttributeMappings
            .Include(m => m.ProductAttribute)
            .Include(m => m.ProductAttributeValue)
            .Where(m => m.ProductId == product.Id)
            .ToListAsync(cancellationToken);

        var dto = new ProductForUpdateDto
        {
            Id = product.Id,
            NameEn = product.NameEn,
            NameAr = product.NameAr,
            DescriptionEn = product.DescriptionEn,
            DescriptionAr = product.DescriptionAr,
            SKU = product.SKU,
            CategoryId = product.CategoryId,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            AllowBackorder = product.AllowBackorder,
            Brand = product.Brand,
            MainImagePath = product.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
                ?? "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500",
            Images = product.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageForUpdateDto
                {
                    Id = i.Id,
                    Path = i.Path,
                    IsMain = i.IsMain,
                    SortOrder = i.SortOrder
                }).ToList(),
            AllMappings = mappings
                .Select(m => new ProductAttributeMappingForUpdateDto
                {
                    MappingId = m.Id,
                    AttributeId = m.ProductAttributeId,
                    AttributeName = m.ProductAttribute.Name,
                    ValueId = m.ProductAttributeValueId,
                    Value = m.ProductAttributeValue?.Value
                })
                .OrderBy(x => x.AttributeName)
                .ThenBy(x => x.Value)
                .ToList()
        };

        // Group mappings by attribute
        var attrGroups = mappings
            .GroupBy(m => m.ProductAttributeId);

        foreach (var g in attrGroups)
        {
            var attribute = g.First().ProductAttribute;
            if (attribute == null) continue;

            // All existing values for attribute
            var allValues = await _context.ProductAttributeValues
                .Where(v => v.ProductAttributeId == attribute.Id)
                .ToListAsync(cancellationToken);

            var selectedValueIds = g
                .Where(m => m.ProductAttributeValueId != null)
                .Select(m => m.ProductAttributeValueId!.Value)
                .Distinct()
                .ToHashSet();

            var attrDto = new ProductAttributeForUpdateDto
            {
                AttributeId = attribute.Id,
                AttributeName = attribute.Name,
                HasNullMapping = g.Any(m => m.ProductAttributeValueId == null),
                Values = allValues
                    .Select(v => new ProductAttributeValueForUpdateDto
                    {
                        Id = v.Id,
                        Value = v.Value,
                        IsSelected = selectedValueIds.Contains(v.Id)
                    })
                    .OrderBy(v => v.Value)
                    .ToList(),
                Mappings = g
                    .Select(m => new ProductAttributeMappingForUpdateDto
                    {
                        MappingId = m.Id,
                        AttributeId = m.ProductAttributeId,
                        AttributeName = attribute.Name,
                        ValueId = m.ProductAttributeValueId,
                        Value = m.ProductAttributeValue?.Value
                    })
                    .OrderBy(x => x.Value)
                    .ToList()
            };

            dto.Attributes.Add(attrDto);
        }

        return Result<ProductForUpdateDto>.Success(dto);
    }
}