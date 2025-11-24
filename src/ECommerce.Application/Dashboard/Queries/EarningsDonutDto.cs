namespace ECommerce.Application.Dashboard.Queries;

public sealed class EarningsDonutDto
{
    public int Year { get; set; }
    public int Month { get; set; }

    public decimal ThisMonthTotal { get; set; }
    public decimal LastMonthTotal { get; set; }
    public decimal PercentChange { get; set; }

    public List<string> Labels { get; set; } = new();
    public List<decimal> Series { get; set; } = new();
}
