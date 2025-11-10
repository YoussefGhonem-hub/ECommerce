using System.ComponentModel.DataAnnotations;

namespace ECommerce.Domain.Common;
public class BaseEntity
{
    [Key]
    public Guid Id { get; set; }
    public DateTimeOffset CreatedDate { get; set; } = DateTime.UtcNow;
    public BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}