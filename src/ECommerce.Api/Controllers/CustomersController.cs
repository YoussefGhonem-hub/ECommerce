using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/customers")]
[Authorize(Roles = "Admin")]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var users = await _context.Users
            .Select(u => new { u.Id, u.UserName, u.Email, u.PhoneNumber })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        var orders = await _context.Orders.Where(o => o.UserId == id).ToListAsync();
        var addresses = await _context.UserAddresses.Where(a => a.UserId == id).ToListAsync();
        return Ok(new { user.Id, user.UserName, user.Email, user.PhoneNumber, orders, addresses });
    }
}
