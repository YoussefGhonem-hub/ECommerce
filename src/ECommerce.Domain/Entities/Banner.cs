using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Banner : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
