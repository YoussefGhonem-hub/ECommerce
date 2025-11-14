using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Shared.CurrentUser;
using MediatR;

namespace ECommerce.Application.UserAddresses.Commands;

public record CreateUserAddressCommand(string Country, string City, string Street, string FullName, string? HouseNo, string MobileNumber) : IRequest<Result<Guid>>;
public class CreateUserAddressHandler : IRequestHandler<CreateUserAddressCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateUserAddressHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateUserAddressCommand request, CancellationToken cancellationToken)
    {


        var address = new UserAddress
        {
            UserId = CurrentUser.Id,
            FullName = request.FullName,
            HouseNo = request.HouseNo,
            MobileNumber = request.MobileNumber,
            Country = request.Country,
            City = request.City,
            Street = request.Street,
        };
        _context.UserAddresses.Add(address);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(address.Id);
    }
}