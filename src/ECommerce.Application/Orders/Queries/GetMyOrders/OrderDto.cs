namespace ECommerce.Application.Orders.Queries.GetMyOrders;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    // Monetary breakdown
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal Total { get; set; }

    // Statuses
    public string Status { get; set; }
    public int PaymentStatus { get; set; }

    // Shipping / coupon / meta
    public Guid? ShippingAddressId { get; set; }
    public Guid? ShippingMethodId { get; set; }
    public string? ShippingMethodName { get; set; }
    public string? ShippingEstimatedTime { get; set; }
    public string? CouponCode { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    // Address snapshot (from UserAddress entity referenced)
    public OrderAddressDto? ShippingAddress { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal LineTotal { get; set; }
    public List<OrderItemAttributeDto> Attributes { get; set; } = new();
}

public class OrderItemAttributeDto
{
    public Guid ProductAttributeId { get; set; }
    public Guid? ProductAttributeValueId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class OrderAddressDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public Guid CityId { get; set; }
    public string CityNameEn { get; set; } = string.Empty;
    public string CityNameAr { get; set; } = string.Empty;
    public Guid CountryId { get; set; }
    public string CountryNameEn { get; set; } = string.Empty;
    public string CountryNameAr { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string? HouseNo { get; set; }
}
