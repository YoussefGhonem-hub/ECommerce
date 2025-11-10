namespace ECommerce.Application.Features.Categories.DTOs;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public bool IsFeatured { get; set; }
}
