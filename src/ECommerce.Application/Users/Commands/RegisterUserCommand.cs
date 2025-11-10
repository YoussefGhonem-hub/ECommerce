using ECommerce.Application.Common;
using ECommerce.Application.Users.Dtos;
using MediatR;

namespace ECommerce.Application.Users.Commands;

public record RegisterUserCommand(RegisterRequest Request) : IRequest<Result<AuthResponse>>;
