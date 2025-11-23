using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductAttributes.Commands;

public record UpdateProductAttributeCommand(
    Guid Id,
    string Name,
    IReadOnlyList<string>? AddValues,        // values to add
    IReadOnlyList<Guid>? RemoveValueIds     // existing value ids to delete
) : IRequest<Result<bool>>;

public class UpdateProductAttributeHandler : IRequestHandler<UpdateProductAttributeCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;
    public UpdateProductAttributeHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<bool>> Handle(UpdateProductAttributeCommand request, CancellationToken ct)
    {
        var attribute = await _context.ProductAttributes
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct);

        if (attribute == null)
            return Result<bool>.Failure("ProductAttribute.NotFound");

        var validation = new Dictionary<string, string[]>();

        var newName = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(newName))
            Append(validation, nameof(request.Name), "Name is required.");
        else
        {
            var nameTaken = await _context.ProductAttributes
                .AnyAsync(a => a.Id != attribute.Id && a.Name.ToLower() == newName.ToLower(), ct);
            if (nameTaken)
                Append(validation, nameof(request.Name), "Another attribute already uses this name.");
        }

        if (validation.Count > 0)
            return Result<bool>.Validation(validation);

        attribute.Name = newName;

        // Remove values
        if (request.RemoveValueIds is { Count: > 0 })
        {
            var removable = await _context.ProductAttributeValues
                .Where(v => v.ProductAttributeId == attribute.Id && request.RemoveValueIds.Contains(v.Id))
                .ToListAsync(ct);

            if (removable.Count > 0)
                _context.ProductAttributeValues.RemoveRange(removable);
        }

        // Add new values (distinct, skip existing)
        if (request.AddValues is { Count: > 0 })
        {
            var toAdd = request.AddValues
                .Select(v => v?.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (toAdd.Count > 0)
            {
                var existing = await _context.ProductAttributeValues
                    .Where(v => v.ProductAttributeId == attribute.Id)
                    .Select(v => v.Value)
                    .ToListAsync(ct);

                foreach (var val in toAdd)
                {
                    if (!existing.Any(ev => ev.Equals(val, StringComparison.OrdinalIgnoreCase)))
                    {
                        _context.ProductAttributeValues.Add(new Domain.Entities.ProductAttributeValue
                        {
                            ProductAttributeId = attribute.Id,
                            Value = val!
                        });
                    }
                }
            }
        }

        await _context.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private static void Append(IDictionary<string, string[]> bag, string key, string message)
    {
        if (bag.TryGetValue(key, out var arr))
            bag[key] = arr.Concat(new[] { message }).ToArray();
        else
            bag[key] = new[] { message };
    }
}