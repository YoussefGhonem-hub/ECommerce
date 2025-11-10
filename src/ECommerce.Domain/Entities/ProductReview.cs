using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class ProductReview : BaseAuditableEntity
{
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public bool IsApproved { get; set; } = false;
    public string? AdminReply { get; set; }
}
