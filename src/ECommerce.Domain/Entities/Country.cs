using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Country : BaseAuditableEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public ICollection<City> Cities { get; set; } = new List<City>();
}