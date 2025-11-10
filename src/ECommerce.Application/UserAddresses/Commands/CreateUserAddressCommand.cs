using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.UserAddresses.Commands;

public record CreateUserAddressCommand(string UserId, string Country, string City, string Street, string? PostalCode, bool IsDefault) : IRequest<Result<Guid>>;
public class CreateUserAddressHandler : IRequestHandler<CreateUserAddressCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateUserAddressHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateUserAddressCommand request, CancellationToken cancellationToken)
    {
        if (request.IsDefault)
        {
            var existing = await _context.UserAddresses.Where(a => a.UserId == CurrentUser.Id).ToListAsync(cancellationToken);
            foreach (var a in existing) a.IsDefault = false;
        }

        var address = new UserAddress
        {
            UserId = CurrentUser.Id,
            Country = request.Country,
            City = request.City,
            Street = request.Street,
            PostalCode = request.PostalCode,
            IsDefault = request.IsDefault
        };
        _context.UserAddresses.Add(address);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(address.Id);
    }
}