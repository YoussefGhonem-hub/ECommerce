using ECommerce.Application.Favorites.Commands;
using ECommerce.Application.Favorites.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string CurrentUserId => User.FindFirst("uid")!.Value;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var list = await _mediator.Send(new GetMyFavoritesQuery(CurrentUserId));
        return Ok(list);
    }

    [HttpPost("{productId:guid}")]
    public async Task<IActionResult> Add(Guid productId)
    {
        var result = await _mediator.Send(new AddFavoriteCommand(CurrentUserId, productId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Remove(Guid productId)
    {
        var result = await _mediator.Send(new RemoveFavoriteCommand(CurrentUserId, productId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
