using ECommerce.Application.Common;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductAttributes.Queries.GetUserOrderedAttributes;

// Returns all ProductAttributes (and their ProductAttributeValues) created by the specified user (or current user).
public record GetAdminAttributesQuery(Guid? UserId = null) : IRequest<Result<List<UserAdminAttributeDto>>>;

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

            // Attributes created by user
            var attributes = await _context.ProductAttributes
                .AsNoTracking()
                .Where(a => a.CreatedBy == targetUserId)
                .Select(a => new { a.Id, a.Name })
                .ToListAsync(ct);

            if (attributes.Count == 0)
                return Result<List<UserAdminAttributeDto>>.Success(new List<UserAdminAttributeDto>());

            var attributeIds = attributes.Select(a => a.Id).ToList();

            // Values created by same user (change filter to remove CreatedBy to include all values)
            var values = await _context.ProductAttributeValues
                .AsNoTracking()
                .Where(v => attributeIds.Contains(v.ProductAttributeId) && v.CreatedBy == targetUserId)
                .Select(v => new { v.Id, v.Value, v.ProductAttributeId })
                .ToListAsync(ct);

            var grouped = attributes
                .OrderBy(a => a.Name)
                .Select(a => new UserAdminAttributeDto
                {
                    AttributeId = a.Id,
                    AttributeName = a.Name,
                    HasNullMapping = false,
                    Values = values
                        .Where(v => v.ProductAttributeId == a.Id)
                        .OrderBy(v => v.Value)
                        .Select(v => new UserAdminAttributeValueDto
                        {
                            Id = v.Id,
                            Value = v.Value
                        })
                        .ToList()
                })
                .ToList();

            return Result<List<UserAdminAttributeDto>>.Success(grouped);
        }
        catch (OperationCanceledException)
        {
            throw; // honor cancellation
        }
        catch
        {
            return Result<List<UserAdminAttributeDto>>.Failure("Attributes.FetchFailed");
        }
    }
}

