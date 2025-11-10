using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("sales-by-day")]
    public async Task<IActionResult> SalesByDay()
    {
        var data = await _context.Orders
            .GroupBy(o => o.CreatedDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Total) })
            .OrderBy(x => x.Date)
            .ToListAsync();
        return Ok(data);
    }

    [HttpGet("sales-by-product")]
    public async Task<IActionResult> SalesByProduct()
    {
        var data = await _context.OrderItems
            .Include(oi => oi.Product)
            .GroupBy(oi => new { oi.ProductId, oi.Product!.NameEn })
            .Select(g => new { g.Key.ProductId, g.Key.NameEn, Total = g.Sum(x => x.LineTotal) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();
        return Ok(data);
    }

    [HttpGet("coupon-usage")]
    public async Task<IActionResult> CouponUsage()
    {
        var data = await _context.Coupons
            .Select(c => new { c.Code, c.TimesUsed })
            .OrderByDescending(x => x.TimesUsed)
            .ToListAsync();
        return Ok(data);
    }
}
