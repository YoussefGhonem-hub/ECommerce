using ECommerce.Application.Common;
using ECommerce.Application.Features.Products.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Products.Queries.GetProducts;

public record GetProductsQuery(string? Search, Guid? CategoryId, decimal? MinPrice, decimal? MaxPrice, string? Color, bool BestRating, int PageNumber = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ProductDto>>>;
