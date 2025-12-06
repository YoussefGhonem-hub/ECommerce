using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using ECommerce.Shared.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductAttributes.Queries.GetUserOrderedAttributes;

// Returns a flat list of attributes (with their values) created by the user.
// Filtering and sorting remain, paging is removed.
public class GetAdminAttributesQuery : BaseFilterDto, IRequest<Result<List<UserAdminAttributeDto>>>
{
    public Guid? UserId { get; set; }
    public string? Search { get; set; } // optional filter on attribute Name
}

public class GetAdminAttributesHandler : IRequestHandler<GetAdminAttributesQuery, Result<List<UserAdminAttributeDto>>>
{
    private readonly ApplicationDbContext _context;
    public GetAdminAttributesHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<List<UserAdminAttributeDto>>> Handle(GetAdminAttributesQuery request, CancellationToken ct)
    {
        try
        {
            var targetUserId = request.UserId ?? CurrentUser.Id;
            if (targetUserId is null)
                return Result<List<UserAdminAttributeDto>>.Failure("User.NotAuthenticated");

            // Base attribute query
            var baseQuery = _context.ProductAttributes
                .AsNoTracking()
                .Where(a => a.CreatedBy == targetUserId);

            // Search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim().ToLower();
                baseQuery = baseQuery.Where(a => a.Name.ToLower().Contains(term));
            }

            // Sorting (default by Name ascending). Only "Name" supported.
            if (string.IsNullOrWhiteSpace(request.Sort) || !request.Sort.Trim().Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = request.Descending
                    ? baseQuery.OrderByDescending(a => a.Name)
                    : baseQuery.OrderBy(a => a.Name);
            }
            else
            {
                baseQuery = request.Descending
                    ? baseQuery.OrderByDescending(a => a.Name)
                    : baseQuery.OrderBy(a => a.Name);
            }

            // Materialize all attributes (no paging)
            var attributes = await baseQuery
                .Select(a => new { a.Id, a.Name })
                .ToListAsync(ct);

            if (attributes.Count == 0)
                return Result<List<UserAdminAttributeDto>>.Success(new List<UserAdminAttributeDto>(0));

            var attributeIds = attributes.Select(a => a.Id).ToList();

            // Load values for all attributes in the result set
            var values = await _context.ProductAttributeValues
                .AsNoTracking()
                .Where(v => attributeIds.Contains(v.ProductAttributeId))
                .OrderBy(v => v.Value)
                .Select(v => new { v.Id, v.Value, v.ProductAttributeId })
                .ToListAsync(ct);

            var items = attributes
                .Select(a => new UserAdminAttributeDto
                {
                    AttributeId = a.Id,
                    AttributeName = a.Name,
                    Values = values
                        .Where(v => v.ProductAttributeId == a.Id)
                        .Select(v => new UserAdminAttributeValueDto
                        {
                            Id = v.Id,
                            Value = v.Value
                        })
                        .ToList()
                })
                .ToList();

            return Result<List<UserAdminAttributeDto>>.Success(items);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Result<List<UserAdminAttributeDto>>.Failure("Attributes.FetchFailed");
        }
    }
}