using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Faq : BaseAuditableEntity
{
    public Guid FaqCategoryId { get; set; }
    public FaqCategory? Category { get; set; }

    public string QuestionEn { get; set; } = string.Empty;
    public string QuestionAr { get; set; } = string.Empty;
    public string AnswerEn { get; set; } = string.Empty;
    public string AnswerAr { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}