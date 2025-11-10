using MediatR;
using ECommerce.Application.Features.Common;

namespace ECommerce.Application.Features.Categories.Queries.GetCategories;

public record GetCategoriesQuery() : IRequest<Result<List<CategoryDto>>>;
