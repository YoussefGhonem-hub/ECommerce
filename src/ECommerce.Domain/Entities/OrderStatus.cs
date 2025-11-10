namespace ECommerce.Domain.Entities;

public enum OrderStatus
{
    Pending = 1,
    PaymentPending = 2,
    Paid = 3,
    Processing = 4,
    Packed = 5,
    Shipped = 6,
    Delivered = 7,
    Cancelled = 8,
    Returned = 9
}
