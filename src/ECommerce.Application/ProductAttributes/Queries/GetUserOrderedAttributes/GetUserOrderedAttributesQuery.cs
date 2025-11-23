using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using ECommerce.Shared.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductAttributes.Queries.GetUserOrderedAttributes;

// Pagination using BaseFilterDto (PageIndex, PageSize, Sort, Descending)
// Returns a PagedResult of attributes (with their values) created by the user.
public class GetAdminAttributesQuery : BaseFilterDto, IRequest<Result<PagedResult<UserAdminAttributeDto>>>
{
    public Guid? UserId { get; set; }
    public string? Search { get; set; } // optional filter on attribute Name
}

public class GetAdminAttributesHandler : IRequestHandler<GetAdminAttributesQuery, Result<PagedResult<UserAdminAttributeDto>>>
{
    private readonly ApplicationDbContext _context;
    public GetAdminAttributesHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<PagedResult<UserAdminAttributeDto>>> Handle(GetAdminAttributesQuery request, CancellationToken ct)
    {
        try
        {
            var targetUserId = request.UserId ?? CurrentUser.Id;
            if (targetUserId is null)
                return Result<PagedResult<UserAdminAttributeDto>>.Failure("User.NotAuthenticated");

            // Normalize paging
            var pageNumber = request.PageIndex <= 0 ? 1 : request.PageIndex;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            // Base attribute query
            var baseQuery = _context.ProductAttributes
                .AsNoTracking()
                .Where(a => a.CreatedBy == targetUserId);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim().ToLower();
                baseQuery = baseQuery.Where(a => a.Name.ToLower().Contains(term));
            }

            // Sorting (default by Name ascending)
            if (string.IsNullOrWhiteSpace(request.Sort))
                baseQuery = request.Descending
                    ? baseQuery.OrderByDescending(a => a.Name)
                    : baseQuery.OrderBy(a => a.Name);
            else
            {
                // Only "Name" is meaningful here; ignore others gracefully
                var sortBy = request.Sort.Trim();
                if (!sortBy.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    sortBy = "Name";

                // Apply direction
                baseQuery = request.Descending
                    ? baseQuery.OrderByDescending(a => a.Name)
                    : baseQuery.OrderBy(a => a.Name);
            }

            var totalCount = await baseQuery.CountAsync(ct);

            if (totalCount == 0)
            {
                return Result<PagedResult<UserAdminAttributeDto>>.Success(
                    PagedResult<UserAdminAttributeDto>.Create(Array.Empty<UserAdminAttributeDto>(), 0, pageNumber, pageSize));
            }

            // Page slice (attributes only)
            var attributeSlice = await baseQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new { a.Id, a.Name })
                .ToListAsync(ct);

            var attributeIds = attributeSlice.Select(a => a.Id).ToList();

            // Load values for attributes in current page (include all their values regardless of CreatedBy)
            var values = await _context.ProductAttributeValues
                .AsNoTracking()
                .Where(v => attributeIds.Contains(v.ProductAttributeId))
                .OrderBy(v => v.Value)
                .Select(v => new { v.Id, v.Value, v.ProductAttributeId })
                .ToListAsync(ct);

            var items = attributeSlice
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

            var paged = PagedResult<UserAdminAttributeDto>.Create(items, totalCount, pageNumber, pageSize);
            return Result<PagedResult<UserAdminAttributeDto>>.Success(paged);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Result<PagedResult<UserAdminAttributeDto>>.Failure("Attributes.FetchFailed");
        }
    }
}

