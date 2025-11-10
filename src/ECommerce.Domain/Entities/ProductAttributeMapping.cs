using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

//Map a product to attribute+value to show on PDP and support filters.
public class ProductAttributeMapping : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid ProductAttributeId { get; set; }
    public ProductAttribute ProductAttribute { get; set; }
    public Guid? ProductAttributeValueId { get; set; }
    public ProductAttributeValue? ProductAttributeValue { get; set; }
}
