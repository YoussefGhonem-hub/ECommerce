using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Dashboard.Queries;
public sealed record GetDashboardStatsQuery() : IRequest<Result<DashboardStatsDto>>;
public class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    private readonly ApplicationDbContext _context;

    public GetDashboardStatsHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var totalOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var totalRevenue = await _context.Orders
                .AsNoTracking()
                .SumAsync(o => (decimal?)o.Total ?? 0m, cancellationToken);

            var totalProducts = await _context.Products
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var totalUsersApprox = await _context.UserAddresses
                .AsNoTracking()
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            // Top products: group by ProductId, then join to Products to get Name
            var topProducts = await _context.OrderItems
                .AsNoTracking()
                .GroupBy(oi => oi.ProductId)
                .Select(g => new { ProductId = g.Key, OrdersCount = g.Count() })
                .OrderByDescending(x => x.OrdersCount)
                .Take(5)
                .Join(
                    _context.Products.AsNoTracking(),
                    g => g.ProductId,
                    p => p.Id,
                    (g, p) => new TopProductDto
                    {
                        ProductId = p.Id,
                        NameEn = p.NameEn,
                        NameAr = p.NameAr,
                        OrdersCount = g.OrdersCount
                    })
                .ToListAsync(cancellationToken);

            // Low stock products to populate DTO property
            var lowStockProducts = await _context.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity < 10)
                .OrderBy(p => p.StockQuantity)
                .Select(p => new LowStockProductDto
                {
                    ProductId = p.Id,
                    NameEn = p.NameEn,
                    NameAr = p.NameAr,
                    StockQuantity = p.StockQuantity
                })
                .ToListAsync(cancellationToken);

            var dto = new DashboardStatsDto
            {
                TotalUsers = totalUsersApprox,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalProducts = totalProducts,
                TopProducts = topProducts,
                LowStockProducts = lowStockProducts
            };

            return Result<DashboardStatsDto>.Success(dto);
        }
        catch (OperationCanceledException)
        {
            return Result<DashboardStatsDto>.Failure("Operation was cancelled.");
        }
        catch (Exception ex)
        {
            return Result<DashboardStatsDto>.Failure("Failed to load dashboard stats.", ex.Message);
        }
    }
}
