using ECommerce.Application.Reviews.Commands;
using ECommerce.Application.Reviews.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var list = await _mediator.Send(new GetProductReviewsQuery(productId));
        return Ok(list);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Add(AddReviewRequest request)
    {
        var userId = User.FindFirst("uid")!.Value;
        var result = await _mediator.Send(new AddReviewCommand(userId, request.ProductId, request.Rating, request.Comment));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{reviewId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid reviewId)
    {
        var userId = User.FindFirst("uid")!.Value;
        var result = await _mediator.Send(new DeleteReviewCommand(userId, reviewId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}

public record AddReviewRequest(Guid ProductId, int Rating, string? Comment);
