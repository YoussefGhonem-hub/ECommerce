namespace ECommerce.Application.Products.Queries.GetProductByIdForUpdate;

public class ProductForUpdateDto
{
    public Guid Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string SKU { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool AllowBackorder { get; set; }
    public string? Brand { get; set; }

    public string? MainImagePath { get; set; }
    public List<ProductImageForUpdateDto> Images { get; set; } = new();

    public List<ProductAttributeForUpdateDto> Attributes { get; set; } = new();

}

public class ProductImageForUpdateDto
{
    public Guid Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int SortOrder { get; set; }
}

public class ProductAttributeForUpdateDto
{
    public Guid AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public List<ProductAttributeValueForUpdateDto> Values { get; set; } = new();
}

public class ProductAttributeValueForUpdateDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
