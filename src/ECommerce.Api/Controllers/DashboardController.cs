using ECommerce.Application.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct = default)
    {
        var stats = await _mediator.Send(new GetDashboardStatsQuery(), ct);
        return stats.Succeeded ? Ok(stats.Data) : BadRequest(stats.Errors);
    }

    [HttpGet("earnings-donut")]
    public async Task<IActionResult> GetEarningsDonut(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int top = 4,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEarningsDonutQuery(year, month, top), ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("revenue-report")]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] string? granularity = "month",
        [FromQuery] string? currency = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRevenueReportQuery(start, end, granularity, currency), ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }
}
