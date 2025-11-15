using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class City : BaseAuditableEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;

    public Guid CountryId { get; set; }
    public Country? Country { get; set; }
}