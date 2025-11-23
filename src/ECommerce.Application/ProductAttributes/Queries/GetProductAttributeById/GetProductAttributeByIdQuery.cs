using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductAttributes.Queries.GetProductAttributeById;

public record GetProductAttributeByIdQuery(Guid Id) : IRequest<Result<ProductAttributeDto>>;

public class GetProductAttributeByIdHandler : IRequestHandler<GetProductAttributeByIdQuery, Result<ProductAttributeDto>>
{
    private readonly ApplicationDbContext _context;
    public GetProductAttributeByIdHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<ProductAttributeDto>> Handle(GetProductAttributeByIdQuery request, CancellationToken ct)
    {
        var attribute = await _context.ProductAttributes
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct);

        if (attribute == null)
            return Result<ProductAttributeDto>.Failure("ProductAttribute.NotFound");

        var values = await _context.ProductAttributeValues
            .Where(v => v.ProductAttributeId == attribute.Id)
            .OrderBy(v => v.Value)
            .Select(v => new ProductAttributeValueDto
            {
                Id = v.Id,
                Value = v.Value
            })
            .ToListAsync(ct);

        var dto = new ProductAttributeDto
        {
            Id = attribute.Id,
            Name = attribute.Name,
            Values = values
        };

        return Result<ProductAttributeDto>.Success(dto);
    }
}

public class ProductAttributeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ProductAttributeValueDto> Values { get; set; } = new();
}

public class ProductAttributeValueDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
}