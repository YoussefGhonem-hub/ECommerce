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

        var selected = NormalizeSelectedAttributes(request.Attributes);

        var allowed = await GetAllowedMappingsAsync(request.ProductId, cancellationToken);
        var validationError = ValidateSelections(selected, allowed);
        if (validationError is not null)
            return Result<bool>.Failure(validationError);

        var snapshotByPair = BuildSnapshotDictionary(allowed);

        var cart = await GetOrCreateCartAsync(cancellationToken);

        var existingItem = FindExistingItem(cart, request.ProductId, selected);

        if (existingItem is null)
        {
            var newItem = CreateCartItem(cart, request.ProductId, request.Quantity, selected, snapshotByPair);
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

    #region MyRegion
    private static IReadOnlyDictionary<Guid, Guid?> NormalizeSelectedAttributes(IReadOnlyList<SelectedAttributeDto>? attributes) =>
        (attributes ?? Array.Empty<SelectedAttributeDto>())
            .GroupBy(a => a.AttributeId)
            .Select(g => g.First())
            .ToDictionary(x => x.AttributeId, x => x.ValueId);

    private async Task<List<AllowedMap>> GetAllowedMappingsAsync(Guid productId, CancellationToken ct) =>
        await _context.ProductAttributeMappings
            .Where(m => m.ProductId == productId)
            .Select(m => new AllowedMap(
                m.ProductAttributeId,
                m.ProductAttributeValueId,
                m.ProductAttribute.Name,
                m.ProductAttributeValue != null ? m.ProductAttributeValue.Value : null))
            .ToListAsync(ct);

    private static string? ValidateSelections(
        IReadOnlyDictionary<Guid, Guid?> selected,
        List<AllowedMap> allowed)
    {
        foreach (var sel in selected)
        {
            var match = allowed.FirstOrDefault(a =>
                a.ProductAttributeId == sel.Key &&
                a.ProductAttributeValueId == sel.Value);
        }

        return null;
    }

    private static Dictionary<(Guid AttributeId, Guid? ValueId), AllowedMap> BuildSnapshotDictionary(List<AllowedMap> allowed) =>
        allowed.ToDictionary(k => (k.ProductAttributeId, k.ProductAttributeValueId));

    private async Task<Cart> GetOrCreateCartAsync(CancellationToken ct)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Attributes)
            .FirstOrDefaultAsync(c =>
                    (CurrentUser.UserId != null && c.UserId == CurrentUser.Id) ||
                    (CurrentUser.GuestId != null && c.GuestId == CurrentUser.GuestId),
                ct);

        if (cart is null)
        {
            cart = new Cart
            {
                UserId = CurrentUser.Id ?? null,
                GuestId = CurrentUser.GuestId
            };
            _context.Carts.Add(cart);
        }

        return cart;
    }

    private static CartItem? FindExistingItem(Cart cart, Guid productId, IReadOnlyDictionary<Guid, Guid?> selected) =>
        cart.Items.FirstOrDefault(i =>
            i.ProductId == productId &&
            AttributesEqual(i.Attributes, selected));

    private CartItem CreateCartItem(
        Cart cart,
        Guid productId,
        int quantity,
        IReadOnlyDictionary<Guid, Guid?> selected,
        IReadOnlyDictionary<(Guid AttributeId, Guid? ValueId), AllowedMap> snapshotByPair)
    {
        var item = new CartItem
        {
            CartId = cart.Id, // set navigation
            ProductId = productId,
            Quantity = quantity
        };

        foreach (var (attrId, valId) in selected)
        {
            var snap = snapshotByPair[(attrId, valId)];
            var attribute = new CartItemAttribute
            {
                CartItemId = item.Id,
                ProductAttributeId = attrId,
                ProductAttributeValueId = valId,
                AttributeName = snap.AttributeName,
                Value = snap.Value
            };

            item.Attributes.Add(attribute);
        }

        return item;
    }

    private readonly record struct AllowedMap(
        Guid ProductAttributeId,
        Guid? ProductAttributeValueId,
        string AttributeName,
        string? Value);
    #endregion

}