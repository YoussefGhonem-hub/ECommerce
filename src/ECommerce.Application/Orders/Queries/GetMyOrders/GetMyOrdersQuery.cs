using ECommerce.Application.Common;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetMyOrders;

public record GetMyOrdersQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<OrderDto>>>;
