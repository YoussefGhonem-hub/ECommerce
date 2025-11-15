using ECommerce.Application.ProductSettings.Commands.CreateProductSetting;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/product-settings")]
[Authorize(Roles = "Admin")]
public class ProductSettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ApplicationDbContext _context;

    public ProductSettingsController(IMediator mediator, ApplicationDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductSettingCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        if (!result.Succeeded) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var list = await _context.ProductSettings
            .Include(ps => ps.Products)
            .ToListAsync();
        return Ok(list);
    }
}