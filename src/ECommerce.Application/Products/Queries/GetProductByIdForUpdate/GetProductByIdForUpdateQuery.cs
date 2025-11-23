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

    public async Task<Result<ProductForUpdateDto>> Handle(GetProductByIdForUpdateQuery request, CancellationToken ct)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (product is null)
            return Result<ProductForUpdateDto>.Failure("Product.NotFound");

        var mappings = await _context.ProductAttributeMappings
            .Include(m => m.ProductAttribute)
            .Include(m => m.ProductAttributeValue)
            .Where(m => m.ProductId == product.Id)
            .ToListAsync(ct);

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
                }).ToList()
        };

        foreach (var g in mappings.GroupBy(m => m.ProductAttributeId))
        {
            var attr = g.First().ProductAttribute;
            if (attr == null) continue;

            var selectedValueIds = g
                .Where(m => m.ProductAttributeValueId != null)
                .Select(m => m.ProductAttributeValueId!.Value)
                .Distinct()
                .ToHashSet();

            // Load ALL allowed values for attribute so UI can show unselected ones
            var allValues = await _context.ProductAttributeValues
                .Where(v => v.ProductAttributeId == attr.Id)
                .ToListAsync(ct);

            dto.Attributes.Add(new ProductAttributeForUpdateDto
            {
                AttributeId = attr.Id,
                AttributeName = attr.Name,
                Values = allValues
                    .Select(v => new ProductAttributeValueForUpdateDto
                    {
                        Id = v.Id,
                        Value = v.Value,
                        IsSelected = selectedValueIds.Contains(v.Id)
                    })
                    .OrderBy(v => v.Value)
                    .ToList()
            });
        }

        return Result<ProductForUpdateDto>.Success(dto);
    }
}