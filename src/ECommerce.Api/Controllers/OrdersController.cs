using ECommerce.Application.Orders.Queries.GetMyOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string CurrentUserId => User.FindFirst("uid")!.Value;

    [HttpGet("me")]
    public async Task<IActionResult> MyOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetMyOrdersQuery(CurrentUserId, pageNumber, pageSize));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
