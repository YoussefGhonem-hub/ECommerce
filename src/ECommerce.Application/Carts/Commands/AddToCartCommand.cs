using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Carts.Commands;

// Accept selected attributes for the product being added
public record AddToCartCommand(
    Guid ProductId,
    int Quantity,
    IReadOnlyList<SelectedAttributeDto>? Attributes = null
) : IRequest<Result<bool>>;

// Attribute + optional value (predefined values may be null for free-form attributes)
public record SelectedAttributeDto(Guid AttributeId, Guid? ValueId);

public class AddToCartHandler : IRequestHandler<AddToCartCommand, Result<bool>>
{
    private readonly ApplicationDbContext _context;

    public AddToCartHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            return Result<bool>.Failure("Quantity must be greater than zero.");

        // Normalize selected attributes (dedupe by AttributeId, keep first)
        var normalizedSelected = (request.Attributes ?? Array.Empty<SelectedAttributeDto>())
            .GroupBy(a => a.AttributeId)
            .Select(g => g.First())
            .ToDictionary(x => x.AttributeId, x => x.ValueId);

        // Validate selections against allowed mappings for the product and prepare snapshots
        var allowed = await _context.ProductAttributeMappings
            .Where(m => m.ProductId == request.ProductId)
            .Select(m => new
            {
                m.ProductAttributeId,
                m.ProductAttributeValueId,
                AttributeName = m.ProductAttribute.Name,
                Value = m.ProductAttributeValue != null ? m.ProductAttributeValue.Value : null
            })
            .ToListAsync(cancellationToken);

        foreach (var sel in normalizedSelected)
        {
            var match = allowed.FirstOrDefault(a =>
                a.ProductAttributeId == sel.Key &&
                a.ProductAttributeValueId == sel.Value);

            if (match is null)
            {
                return Result<bool>.Failure(
                    $"Invalid attribute selection. AttributeId={sel.Key}, ValueId={(sel.Value?.ToString() ?? "null")} is not allowed for this product.");
            }
        }

        // Build snapshots to store in cart item (only for provided selections)
        var snapshotByPair = allowed
            .ToDictionary(k => (k.ProductAttributeId, k.ProductAttributeValueId), v => v);

        // Load or create cart with items and their selected attributes
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Attributes)
            .FirstOrDefaultAsync(c =>
                    (CurrentUser.UserId != null && c.UserId == CurrentUser.Id) ||
                    (CurrentUser.GuestId != null && c.GuestId == CurrentUser.GuestId),
                cancellationToken);

        if (cart is null)
        {
            cart = new Cart
            {
                UserId = CurrentUser.Id ?? null,
                GuestId = CurrentUser.GuestId
            };
            _context.Carts.Add(cart);
        }

        // Try to find an existing line with same product and same attribute selections
        var existingItem = cart.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId &&
            AttributesEqual(i.Attributes, normalizedSelected));

        if (existingItem is null)
        {
            var newItem = new CartItem
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };

            foreach (var (attrId, valId) in normalizedSelected)
            {
                var snap = snapshotByPair[(attrId, valId)];
                newItem.Attributes.Add(new CartItemAttribute
                {
                    ProductAttributeId = attrId,
                    ProductAttributeValueId = valId,
                    AttributeName = snap.AttributeName,
                    Value = snap.Value
                });
            }

            cart.Items.Add(newItem);
        }
        else
        {
            existingItem.Quantity += request.Quantity;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    // Compare attribute sets ignoring order
    private static bool AttributesEqual(ICollection<CartItemAttribute> existing, IReadOnlyDictionary<Guid, Guid?> selected)
    {
        if ((existing?.Count ?? 0) != selected.Count)
            return false;

        foreach (var a in existing)
        {
            if (!selected.TryGetValue(a.ProductAttributeId, out var valueId))
                return false;

            if (a.ProductAttributeValueId != valueId)
                return false;
        }

        return true;
    }
}