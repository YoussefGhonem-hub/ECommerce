using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ECommerce.Application.Dashboard.Queries;

public sealed record GetRevenueReportQuery(
    DateTime? Start = null,
    DateTime? End = null,
    string? Granularity = "month",   // "day" | "week" | "month"
    string? Currency = null
) : IRequest<Result<RevenueReportDto>>;

public sealed class RevenueReportDto
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public List<string> Labels { get; set; } = new();
    public List<RevenueSeriesDto> Series { get; set; } = new();
    public RevenueTotalsDto Totals { get; set; } = new();
    public RevenueAnalyticsDto AnalyticsData { get; set; } = new();
    public string? Currency { get; set; }
}

public sealed class RevenueSeriesDto
{
    public string Name { get; set; } = string.Empty;
    public List<decimal> Data { get; set; } = new();
}

public sealed class RevenueTotalsDto
{
    public decimal EarningTotal { get; set; }
    public decimal ExpenseTotal { get; set; }
}

public sealed class RevenueAnalyticsDto
{
    public decimal CurrentBudget { get; set; }
    public decimal TotalBudget { get; set; }
}

public class GetRevenueReportHandler : IRequestHandler<GetRevenueReportQuery, Result<RevenueReportDto>>
{
    private readonly ApplicationDbContext _context;
    public GetRevenueReportHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<RevenueReportDto>> Handle(GetRevenueReportQuery request, CancellationToken ct)
    {
        // Validate granularity
        var granularity = (request.Granularity ?? "month").ToLowerInvariant();
        if (granularity is not ("day" or "week" or "month"))
            return Result<RevenueReportDto>.Failure("Invalid granularity. Use day|week|month.");

        // Establish range
        var now = DateTime.UtcNow;
        var start = request.Start?.Date ?? new DateTime(now.Year, 1, 1);
        var end = request.End?.Date ?? now.Date;
        if (end < start)
            return Result<RevenueReportDto>.Failure("End date cannot be before Start date.");

        // Pull paid orders in range
        var paidOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => (int)o.PaymentStatus == 2 /* Paid */ &&
                        o.CreatedDate >= start &&
                        o.CreatedDate < end.AddDays(1)) // inclusive end
            .Select(o => new
            {
                o.Total,
                o.SubTotal,
                o.DiscountTotal,
                o.TaxTotal,
                o.ShippingTotal,
                Created = o.CreatedDate.UtcDateTime
            })
            .ToListAsync(ct);

        // Group buckets
        var earningsBuckets = new Dictionary<string, decimal>();
        var expenseBuckets = new Dictionary<string, decimal>();

        // Expense heuristic: (DiscountTotal + TaxTotal + ShippingTotal)
        // Adjust this formula to real cost-of-goods when available.
        foreach (var o in paidOrders)
        {
            string key = granularity switch
            {
                "day" => o.Created.ToString("yyyy-MM-dd"),
                "week" => GetIsoWeekKey(o.Created),
                _ => o.Created.ToString("yyyy-MM") // month
            };

            if (!earningsBuckets.ContainsKey(key)) earningsBuckets[key] = 0m;
            if (!expenseBuckets.ContainsKey(key)) expenseBuckets[key] = 0m;

            earningsBuckets[key] += o.Total;
            expenseBuckets[key] += (o.DiscountTotal + o.TaxTotal + o.ShippingTotal);
        }

        // Build ordered full range with gaps filled (zeros)
        var labels = new List<string>();
        var earningData = new List<decimal>();
        var expenseData = new List<decimal>();

        if (granularity == "day")
        {
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                var key = d.ToString("yyyy-MM-dd");
                labels.Add(d.ToString("dd MMM"));
                earningData.Add(earningsBuckets.TryGetValue(key, out var e) ? e : 0m);
                expenseData.Add(expenseBuckets.TryGetValue(key, out var x) ? x : 0m);
            }
        }
        else if (granularity == "week")
        {
            // Iterate by weeks from start Monday to end Sunday (ISO weeks)
            var cal = CultureInfo.InvariantCulture.Calendar;
            var isoStart = start.AddDays(-(int)GetIsoDayOfWeek(start) + 1);
            for (var d = isoStart; d <= end; d = d.AddDays(7))
            {
                var key = GetIsoWeekKey(d);
                var weekLabel = key; // e.g. 2025-W07
                labels.Add(weekLabel);
                earningData.Add(earningsBuckets.TryGetValue(key, out var e) ? e : 0m);
                expenseData.Add(expenseBuckets.TryGetValue(key, out var x) ? x : 0m);
            }
        }
        else
        {
            // Month granularity: iterate months
            var iter = new DateTime(start.Year, start.Month, 1);
            var endMonth = new DateTime(end.Year, end.Month, 1);
            while (iter <= endMonth)
            {
                var key = iter.ToString("yyyy-MM");
                labels.Add(iter.ToString("MMM"));
                earningData.Add(earningsBuckets.TryGetValue(key, out var e) ? e : 0m);
                expenseData.Add(expenseBuckets.TryGetValue(key, out var x) ? x : 0m);
                iter = iter.AddMonths(1);
            }
        }

        var earningTotal = earningData.Sum();
        var expenseTotal = expenseData.Sum();

        var dto = new RevenueReportDto
        {
            Year = granularity == "month" ? start.Year : null,
            Month = granularity == "day" && start.Month == end.Month ? start.Month : null,
            Currency = request.Currency,
            Labels = labels,
            Series = new List<RevenueSeriesDto>
            {
                new() { Name = "Earning", Data = earningData },
                new() { Name = "Expense", Data = expenseData }
            },
            Totals = new RevenueTotalsDto
            {
                EarningTotal = earningTotal,
                ExpenseTotal = expenseTotal
            },
            AnalyticsData = new RevenueAnalyticsDto
            {
                CurrentBudget = earningTotal,      // Placeholder: current budget as earnings
                TotalBudget = earningTotal * 4m    // Placeholder: arbitrary projection
            }
        };

        return Result<RevenueReportDto>.Success(dto);
    }

    private static string GetIsoWeekKey(DateTime date)
    {
        var cal = CultureInfo.InvariantCulture.Calendar;
        var week = cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return $"{date.Year}-W{week:D2}";
    }

    private static DayOfWeek GetIsoDayOfWeek(DateTime date)
    {
        // ISO: Monday=1 ... Sunday=7
        var dow = date.DayOfWeek;
        return dow == DayOfWeek.Sunday ? DayOfWeek.Sunday : dow;
    }
}