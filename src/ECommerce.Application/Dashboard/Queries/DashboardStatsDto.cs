namespace ECommerce.Application.Dashboard.Queries;

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalProducts { get; set; }
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<LowStockProductDto> LowStockProducts { get; set; } = new();
}

public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int OrdersCount { get; set; }
}

public class LowStockProductDto
{
    public Guid ProductId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}
