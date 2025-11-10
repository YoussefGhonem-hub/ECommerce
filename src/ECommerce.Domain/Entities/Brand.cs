using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Brand : BaseAuditableEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
