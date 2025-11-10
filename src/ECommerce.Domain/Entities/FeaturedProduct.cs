using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;
//use cases
//•	Marketing placements: Show a curated set of products on the homepage or category landing pages.
//•	Manual curation: Admins can add/remove featured products and adjust DisplayOrder to prioritize certain items.
//•	A/B sections: Build multiple “featured” blocks by filtering and ordering differently or extending the entity if 
public class FeaturedProduct : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public int DisplayOrder { get; set; }
}
