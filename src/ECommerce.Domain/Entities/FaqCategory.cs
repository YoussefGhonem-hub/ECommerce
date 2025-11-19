using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class FaqCategory : BaseAuditableEntity
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    public ICollection<Faq> Faqs { get; set; } = new List<Faq>();
}