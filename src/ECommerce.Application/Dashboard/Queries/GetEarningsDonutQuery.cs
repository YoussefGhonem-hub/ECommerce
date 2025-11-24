using ECommerce.Application.Common;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Dashboard.Queries;

public sealed record GetEarningsDonutQuery(int? Year = null, int? Month = null, int Top = 4)
    : IRequest<Result<EarningsDonutDto>>;

public sealed class GetEarningsDonutHandler : IRequestHandler<GetEarningsDonutQuery, Result<EarningsDonutDto>>
{
    private readonly ApplicationDbContext _context;
    public GetEarningsDonutHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<EarningsDonutDto>> Handle(GetEarningsDonutQuery request, CancellationToken ct)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var year = request.Year ?? now.Year;
            var month = request.Month ?? now.Month;

            var startThis = new DateTimeOffset(new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc));
            var endThis = startThis.AddMonths(1);
            var startLast = startThis.AddMonths(-1);
            var endLast = startThis;

            // Totals (Paid orders only) - compare by underlying int to avoid enum type ambiguity
            var thisMonthTotal = await _context.Orders
                .AsNoTracking()
                .Where(o => (int)o.PaymentStatus == (int)PaymentStatus.Paid &&
                            o.CreatedDate >= startThis && o.CreatedDate < endThis)
                .SumAsync(o => (decimal?)o.Total ?? 0m, ct);

            var lastMonthTotal = await _context.Orders
                .AsNoTracking()
                .Where(o => (int)o.PaymentStatus == (int)PaymentStatus.Paid &&
                            o.CreatedDate >= startLast && o.CreatedDate < endLast)
                .SumAsync(o => (decimal?)o.Total ?? 0m, ct);

            var percentChange = lastMonthTotal == 0m
                ? (thisMonthTotal > 0m ? 100m : 0m)
                : Math.Round(((thisMonthTotal - lastMonthTotal) / lastMonthTotal) * 100m, 1);

            // Distribution by category for the donut (current month, Paid only)
            var categoryTotals = await (
                from oi in _context.OrderItems.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on oi.OrderId equals o.Id
                join p in _context.Products.AsNoTracking() on oi.ProductId equals p.Id
                join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.Id into cc
                from c in cc.DefaultIfEmpty()
                where (int)o.PaymentStatus == (int)PaymentStatus.Paid
                      && o.CreatedDate >= startThis && o.CreatedDate < endThis
                select new
                {
                    CategoryName = c != null ? c.NameEn : "Uncategorized",
                    Amount = (oi.UnitPrice - oi.Discount + oi.Tax) * oi.Quantity
                }
            )
            .GroupBy(x => x.CategoryName)
            .Select(g => new { Label = g.Key, Total = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(ct);

            var top = Math.Max(1, request.Top);
            var labels = new List<string>();
            var series = new List<decimal>();

            if (categoryTotals.Count > 0)
            {
                var topItems = categoryTotals.Take(top).ToList();
                labels.AddRange(topItems.Select(i => i.Label));
                series.AddRange(topItems.Select(i => i.Total));

                var othersTotal = categoryTotals.Skip(top).Sum(i => i.Total);
                if (othersTotal > 0m)
                {
                    labels.Add("Others");
                    series.Add(othersTotal);
                }
            }

            var dto = new EarningsDonutDto
            {
                Year = year,
                Month = month,
                ThisMonthTotal = thisMonthTotal,
                LastMonthTotal = lastMonthTotal,
                PercentChange = percentChange,
                Labels = labels,
                Series = series
            };

            return Result<EarningsDonutDto>.Success(dto);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<EarningsDonutDto>.Failure("Dashboard.EarningsDonut.Failed", ex.Message);
        }
    }
}