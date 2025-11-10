namespace ECommerce.Application.Features.Categories.Queries.GetCategories;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public bool IsFeatured { get; set; }
}
