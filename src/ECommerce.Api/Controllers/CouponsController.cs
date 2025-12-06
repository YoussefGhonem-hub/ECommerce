using ECommerce.Application.Coupons.Commands;
using ECommerce.Application.Coupons.Queries.GetCouponById;
using ECommerce.Application.Coupons.Queries.ListCouponsQuery;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouponsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CouponsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/coupons
    // Optional filters: userId, isActive
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? userId, [FromQuery] bool? isActive, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListCouponsQuery(userId, isActive), ct);
        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    // GET: api/coupons/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCouponByIdQuery(id), ct);
        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    // POST: api/coupons
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCouponCommand dto, CancellationToken ct)
    {

        var result = await _mediator.Send(dto, ct);
        return result.Succeeded
            ? Ok(result)
            : BadRequest(result);
    }

    // PUT: api/coupons/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCouponCommand dto, CancellationToken ct)
    {


        var result = await _mediator.Send(dto, ct);
        return result.Succeeded ? Ok(new { id = result.Data }) : BadRequest(result);
    }

    // DELETE: api/coupons/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteCouponCommand(id), ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }


}