using MediatR;
using ECommerce.Application.Common;

namespace ECommerce.Application.Categories.Queries.GetCategories;

public record GetCategoriesQuery() : IRequest<Result<List<CategoryDto>>>;
