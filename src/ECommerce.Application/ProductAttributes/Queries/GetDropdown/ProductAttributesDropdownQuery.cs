using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductAttributes.Queries.GetDropdown;

// Simple DTOs for dropdown usage
public class ProductAttributeDropdownDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ProductAttributeValueDropdownDto> Values { get; set; } = new();
}

public class ProductAttributeValueDropdownDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

// Query: returns all attributes (optionally filtered by user who created them) with all their values.
// No pagination.
public record GetProductAttributesDropdownQuery(
) : IRequest<Result<List<ProductAttributeDropdownDto>>>;

public class GetProductAttributesDropdownHandler :
    IRequestHandler<GetProductAttributesDropdownQuery, Result<List<ProductAttributeDropdownDto>>>
{
    private readonly ApplicationDbContext _context;
    public GetProductAttributesDropdownHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<List<ProductAttributeDropdownDto>>> Handle(GetProductAttributesDropdownQuery request, CancellationToken ct)
    {
        var targetUserId = CurrentUser.Id;

        // Base attributes query
        var attrQuery = _context.ProductAttributes.AsNoTracking();

        if (targetUserId != null)
            attrQuery = attrQuery.Where(a => a.CreatedBy == targetUserId);


        var attributes = await attrQuery
            .OrderBy(a => a.Name)
            .Select(a => new { a.Id, a.Name })
            .ToListAsync(ct);

        if (attributes.Count == 0)
            return Result<List<ProductAttributeDropdownDto>>.Success(new List<ProductAttributeDropdownDto>());

        var ids = attributes.Select(a => a.Id).ToList();

        // Load all values for those attributes (include all creators)
        var values = await _context.ProductAttributeValues
            .AsNoTracking()
            .Where(v => ids.Contains(v.ProductAttributeId))
            .OrderBy(v => v.Value)
            .Select(v => new { v.Id, v.Value, v.ProductAttributeId })
            .ToListAsync(ct);

        var list = attributes
            .Select(a => new ProductAttributeDropdownDto
            {
                Id = a.Id,
                Name = a.Name,
                Values = values
                    .Where(v => v.ProductAttributeId == a.Id)
                    .Select(v => new ProductAttributeValueDropdownDto
                    {
                        Id = v.Id,
                        Value = v.Value
                    })
                    .ToList()
            })
            .ToList();

        return Result<List<ProductAttributeDropdownDto>>.Success(list);
    }
}