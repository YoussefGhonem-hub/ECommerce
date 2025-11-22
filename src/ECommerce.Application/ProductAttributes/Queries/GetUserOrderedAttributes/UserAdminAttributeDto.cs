namespace ECommerce.Application.ProductAttributes.Queries.GetUserOrderedAttributes;
public class UserAdminAttributeDto
{
    public Guid AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public bool HasNullMapping { get; set; } // retained for compatibility; always false here
    public List<UserAdminAttributeValueDto> Values { get; set; } = new();
}

public class UserAdminAttributeValueDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
}