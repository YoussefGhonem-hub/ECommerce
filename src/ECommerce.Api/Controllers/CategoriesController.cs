using ECommerce.Application.Categories.Commands;
using ECommerce.Application.Categories.Queries.GetCategories;
using ECommerce.Application.Categories.Queries.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoriesController(IMediator mediator) => _mediator = mediator;

    // List all
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetCategoriesQuery getCategoriesQuery, CancellationToken ct)
    {
        var result = await _mediator.Send(getCategoriesQuery, ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    // Get by Id
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id), ct);
        return result.Succeeded ? Ok(result.Data) : NotFound(result.Errors);
    }

    // Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data)
            : BadRequest(result.Errors);
    }

    // Update
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCategoryCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    // Delete
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id), ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }
}