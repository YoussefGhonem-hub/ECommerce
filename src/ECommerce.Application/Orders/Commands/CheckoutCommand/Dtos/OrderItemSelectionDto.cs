namespace ECommerce.Application.Orders.Commands.CheckoutCommand.Dtos;
// Per-cart-item attribute selections
public record OrderItemSelectionDto(Guid CartItemId, IReadOnlyList<SelectedAttributeDto> Attributes);
// Attribute + optional value (for attributes with a predefined value list)
public record SelectedAttributeDto(Guid AttributeId, Guid? ValueId);
public record UserAddressDto(
    Guid CountryId,
    Guid CityId,
    string Street,
    string FullName,
    string MobileNumber,
    string? HouseNo = null,
    bool IsDefault = false);
