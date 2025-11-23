using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductAttributes.Commands;

public record CreateProductAttributeCommand(
    string Name,
    IReadOnlyList<string>? Values // optional initial values
) : IRequest<Result<Guid>>;

public class CreateProductAttributeHandler : IRequestHandler<CreateProductAttributeCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;
    public CreateProductAttributeHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(CreateProductAttributeCommand request, CancellationToken ct)
    {
        var validation = new Dictionary<string, string[]>();

        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            Append(validation, nameof(request.Name), "Name is required.");
        else
        {
            var exists = await _context.ProductAttributes
                .AnyAsync(a => a.Name.ToLower() == name.ToLower(), ct);
            if (exists)
                Append(validation, nameof(request.Name), "Attribute name already exists.");
        }

        if (validation.Count > 0)
            return Result<Guid>.Validation(validation);

        var attribute = new ProductAttribute { Name = name };
        _context.ProductAttributes.Add(attribute);

        // Persist attribute first to keep things simple
        await _context.SaveChangesAsync(ct);

        if (request.Values is { Count: > 0 })
        {
            var distinctVals = request.Values
                .Select(v => v?.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Avoid duplicates in DB
            if (distinctVals.Count > 0)
            {
                var existingVals = await _context.ProductAttributeValues
                    .Where(v => v.ProductAttributeId == attribute.Id)
                    .Select(v => v.Value)
                    .ToListAsync(ct);

                foreach (var val in distinctVals)
                {
                    if (!existingVals.Any(ev => ev.Equals(val, StringComparison.OrdinalIgnoreCase)))
                    {
                        _context.ProductAttributeValues.Add(new ProductAttributeValue
                        {
                            ProductAttributeId = attribute.Id,
                            Value = val!
                        });
                    }
                }
                await _context.SaveChangesAsync(ct);
            }
        }

        return Result<Guid>.Success(attribute.Id);
    }

    private static void Append(IDictionary<string, string[]> bag, string key, string message)
    {
        if (bag.TryGetValue(key, out var arr))
            bag[key] = arr.Concat(new[] { message }).ToArray();
        else
            bag[key] = new[] { message };
    }
}