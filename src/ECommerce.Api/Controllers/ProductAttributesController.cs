using ECommerce.Application.ProductAttributes.Commands;
using ECommerce.Application.ProductAttributes.Queries.GetProductAttributeById;
using ECommerce.Application.ProductAttributes.Queries.GetUserOrderedAttributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductAttributesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductAttributesController(IMediator mediator) => _mediator = mediator;

    // List (existing query you already have)
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAdminAttributes(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAdminAttributesQuery(), ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    // Get by id
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductAttributeByIdQuery(id), ct);
        return result.Succeeded ? Ok(result.Data) : NotFound(result.Errors);
    }

    // Create
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductAttributeCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data)
            : BadRequest(result.Errors);
    }

    // Update
    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProductAttributeCommand body, CancellationToken ct)
    {
        var result = await _mediator.Send(body, ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }
}