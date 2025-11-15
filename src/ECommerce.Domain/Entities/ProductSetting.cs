using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

// Represents an admin-defined discount rule that can target selected products or all products.
public class ProductSetting : BaseAuditableEntity
{
    public ApplicationUser? User { get; set; }
    public Guid? UserId { get; set; }
    public DiscountKind Kind { get; set; }            // Percentage or Fixed
    public decimal Value { get; set; }                // If Percentage => 0–100. If Fixed => amount per unit.
    public bool AppliesToAllProducts { get; set; }    // If true, Products collection can be empty and it still applies.
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product>? Products { get; set; } = new List<Product>();
}

public enum DiscountKind
{
    Percentage = 1,
    FixedAmount = 2
}