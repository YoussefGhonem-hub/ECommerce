using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _context.ProductReviews.OrderByDescending(r => r.CreatedDate).ToListAsync();
        return Ok(list);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var review = await _context.ProductReviews.FirstOrDefaultAsync(r => r.Id == id);
        if (review is null) return NotFound();
        review.IsApproved = true;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReviewReply request)
    {
        var review = await _context.ProductReviews.FirstOrDefaultAsync(r => r.Id == id);
        if (review is null) return NotFound();
        review.AdminReply = request.Reply;
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public record ReviewReply(string Reply);
