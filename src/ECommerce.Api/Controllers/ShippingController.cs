using ECommerce.Domain.Entities;
using ECommerce.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/shipping")]
[Authorize(Roles = "Admin")]
public class ShippingController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ShippingController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("zones")]
    public async Task<IActionResult> CreateZone([FromBody] ShippingZone zone)
    {
        _context.ShippingZones.Add(zone);
        await _context.SaveChangesAsync();
        return Ok(zone.Id);
    }

    [HttpPost("methods")]
    public async Task<IActionResult> CreateMethod([FromBody] ShippingMethod method)
    {
        _context.ShippingMethods.Add(method);
        await _context.SaveChangesAsync();
        return Ok(method.Id);
    }

    [HttpGet("zones")]
    public async Task<IActionResult> GetZones()
    {
        var list = await _context.ShippingZones.Include(z => z.Methods).ToListAsync();
        return Ok(list);
    }
}
