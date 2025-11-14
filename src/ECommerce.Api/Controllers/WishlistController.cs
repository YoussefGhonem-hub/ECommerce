using ECommerce.Application.Favorites.Queries;
using ECommerce.Application.Wishlist.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _services;

    public WishlistController(IMediator mediator, IServiceProvider services)
    {
        _mediator = mediator;
        _services = services;
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddToWishlistCommand request, CancellationToken ct)
    {
        var cmd = new AddToWishlistCommand(request.ProductId, request.GuestId);
        var result = await _mediator.Send(cmd, ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {

        var cmd = new RemoveFromWishlistCommand(id);
        var result = await _mediator.Send(cmd, ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyFavorites([FromQuery] GetMyFavoritesQuery query, CancellationToken ct)
    {
        var result = await _mediator.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

}
