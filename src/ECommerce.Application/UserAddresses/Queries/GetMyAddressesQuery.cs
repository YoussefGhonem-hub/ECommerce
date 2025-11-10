using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.UserAddresses.Queries;

public record GetMyAddressesQuery(string UserId) : IRequest<List<UserAddress>>;
public class GetMyAddressesHandler : IRequestHandler<GetMyAddressesQuery, List<UserAddress>>
{
    private readonly ApplicationDbContext _context;

    public GetMyAddressesHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserAddress>> Handle(GetMyAddressesQuery request, CancellationToken cancellationToken)
    {
        return await _context.UserAddresses
            .Where(a => a.UserId == CurrentUser.Id)
            .OrderByDescending(a => a.IsDefault)
            .ToListAsync(cancellationToken);
    }
}
