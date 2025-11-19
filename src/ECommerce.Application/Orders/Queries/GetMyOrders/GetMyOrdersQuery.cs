using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerce.Application.Orders.Queries.GetMyOrders;

public record GetMyOrdersQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<OrderDto>>>;

public class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, Result<PagedResult<OrderDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetMyOrdersQueryHandler(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<PagedResult<OrderDto>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        if (CurrentUser.Id == Guid.Empty)
            return Result<PagedResult<OrderDto>>.Failure("Not authenticated.");

        var isAdmin = CurrentUser.Roles.Contains("Admin");
        var isCustomer = CurrentUser.Roles.Contains("Customer");

        IQueryable<Domain.Entities.Order> baseQuery;

        if (isAdmin)
        {
            // Orders containing at least one item whose product belongs to this admin (seller perspective)
            baseQuery = _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.Items.Any(i =>
                        i.Product != null &&
                        i.Product.UserId != null &&
                        i.Product.UserId == CurrentUser.Id))
                .OrderByDescending(o => o.CreatedDate);
        }
        else
        {
            // Customer: own orders only
            baseQuery = _context.Orders
                .AsNoTracking()
                .Where(o => o.UserId == CurrentUser.Id)
                .OrderByDescending(o => o.CreatedDate);
        }

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.Attributes)
            .Include(o => o.ShippingAddress)
                .ThenInclude(a => a.City)
                    .ThenInclude(c => c.Country)
            .Include(o => o.ShippingMethod)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,

                SubTotal = o.SubTotal,
                DiscountTotal = o.DiscountTotal,
                TaxTotal = o.TaxTotal,
                ShippingTotal = o.ShippingTotal,
                Total = o.Total,

                Status = o.Status.ToString(),
                PaymentStatus = (int)o.PaymentStatus,

                ShippingAddressId = o.ShippingAddressId,
                ShippingMethodId = o.ShippingMethodId,
                ShippingMethodName = o.ShippingMethod != null ? o.ShippingMethod.CostType.ToString() : null,
                ShippingEstimatedTime = o.ShippingMethod != null ? o.ShippingMethod.EstimatedTime : null,

                CouponCode = o.CouponCode,
                TrackingNumber = o.TrackingNumber,
                Notes = o.Notes,
                CreatedDate = o.CreatedDate,

                ShippingAddress = o.ShippingAddress != null && o.ShippingAddress.City != null && o.ShippingAddress.City.Country != null
                    ? new OrderAddressDto
                    {
                        Id = o.ShippingAddress.Id,
                        FullName = o.ShippingAddress.FullName,
                        CityId = o.ShippingAddress.CityId,
                        CityNameEn = o.ShippingAddress.City.NameEn,
                        CityNameAr = o.ShippingAddress.City.NameAr,
                        CountryId = o.ShippingAddress.City.Country.Id,
                        CountryNameEn = o.ShippingAddress.City.Country.NameEn,
                        CountryNameAr = o.ShippingAddress.City.Country.NameAr,
                        Street = o.ShippingAddress.Street,
                        MobileNumber = o.ShippingAddress.MobileNumber,
                        HouseNo = o.ShippingAddress.HouseNo
                    }
                    : null,

                Items = o.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product != null
                        ? (string.IsNullOrWhiteSpace(i.Product.NameEn)
                            ? (i.Product.NameAr ?? string.Empty)
                            : i.Product.NameEn)
                        : string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Discount = i.Discount,
                    Tax = i.Tax,
                    LineTotal = i.LineTotal,
                    Attributes = i.Attributes.Select(a => new OrderItemAttributeDto
                    {
                        ProductAttributeId = a.ProductAttributeId,
                        ProductAttributeValueId = a.ProductAttributeValueId,
                        AttributeName = a.AttributeName,
                        Value = a.Value
                    }).ToList()
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var paged = PagedResult<OrderDto>.Create(items, total, request.PageNumber, request.PageSize);
        return Result<PagedResult<OrderDto>>.Success(paged);
    }

    private bool IsInRole(string roleName)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null) return false;

        return principal.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role") &&
            string.Equals(c.Value, roleName, StringComparison.OrdinalIgnoreCase));
    }
}