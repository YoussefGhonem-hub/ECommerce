using ECommerce.Domain.Entities;

namespace ECommerce.Application.Orders.Queries.GetMyOrders;

public static class OrderQueryExtensions
{
    // Applies filters defined in GetMyOrdersQuery to the IQueryable<Order>
    public static IQueryable<Order> ApplyOrderFilters(this IQueryable<Order> query, GetMyOrdersQuery filter)
    {
        if (filter == null) return query;

        if (!string.IsNullOrWhiteSpace(filter.OrderNumber))
        {
            var on = filter.OrderNumber.Trim();
            query = query.Where(o => o.OrderNumber.Contains(on));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(term) ||
                (o.TrackingNumber != null && o.TrackingNumber.Contains(term)));
        }

        if (filter.Status.HasValue)
        {
            // Compare underlying int (handles duplicate enum types if any)
            var statusInt = filter.Status.Value;
            query = query.Where(o => (int)o.Status == statusInt);
        }

        if (filter.PaymentStatus.HasValue)
        {
            var payInt = filter.PaymentStatus.Value;
            query = query.Where(o => (int)o.PaymentStatus == payInt);
        }

        if (filter.From.HasValue)
        {
            var from = filter.From.Value;
            query = query.Where(o => o.CreatedDate >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value;
            query = query.Where(o => o.CreatedDate <= to);
        }

        if (filter.MinTotal.HasValue)
        {
            var min = filter.MinTotal.Value;
            query = query.Where(o => o.Total >= min);
        }

        if (filter.MaxTotal.HasValue)
        {
            var max = filter.MaxTotal.Value;
            query = query.Where(o => o.Total <= max);
        }

        if (filter.ProductId.HasValue)
        {
            var pid = filter.ProductId.Value;
            query = query.Where(o => o.Items.Any(i => i.ProductId == pid));
        }

        if (filter.CategoryId.HasValue)
        {
            var cid = filter.CategoryId.Value;
            query = query.Where(o => o.Items.Any(i => i.Product != null && i.Product.CategoryId == cid));
        }

        return query;
    }
}