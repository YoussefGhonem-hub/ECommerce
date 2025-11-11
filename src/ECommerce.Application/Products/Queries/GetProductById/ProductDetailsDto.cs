namespace ECommerce.Application.Products.Queries.GetProductById;

public class ProductDetailsDto
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameAr { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public bool IsInWishlist { get; set; }
    public bool IsInCart { get; set; }

    // Primary image with fallback if null/empty
    public string? MainImagePath { get; set; } =
        "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg?auto=compress&cs=tinysrgb&dpr=1&w=500";

    // Related data
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductReviewDto> Reviews { get; set; } = new();
    public double AverageRating { get; set; }
    public List<ProductAttributeMappingDto> Attributes { get; set; } = new();
}

public class ProductImageDto
{
    public string Path { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int SortOrder { get; set; }
}

public class ProductReviewDto
{
    public Guid Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public string? UserFullName { get; set; }
}

public class ProductAttributeMappingDto
{
    public Guid AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public Guid? ValueId { get; set; }
    public string? Value { get; set; }
}
