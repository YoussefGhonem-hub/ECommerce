namespace ECommerce.Domain.Common;

public class BaseAuditableEntity : BaseEntity
{
    public DateTimeOffset? ModifiedDate { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedDate { get; set; }
    public Guid? DeletedBy { get; set; }
    public void MarkAsCreated(Guid currentUserId)
    {
        CreatedBy = currentUserId;
        CreatedDate = DateTime.UtcNow;
    }

    public void MarkAsNotDeleted()
    {
        IsDeleted = false;
    }

    public void MarkAsDeleted(Guid currentUserId)
    {
        IsDeleted = true;
        DeletedDate = DateTime.UtcNow;
        DeletedBy = currentUserId;
    }

    public void MarkAsModified(Guid currentUserId)
    {
        ModifiedBy = currentUserId;
        ModifiedDate = DateTime.UtcNow;
    }
}