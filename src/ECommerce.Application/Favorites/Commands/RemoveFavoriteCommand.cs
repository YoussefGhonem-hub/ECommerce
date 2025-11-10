using MediatR;
using ECommerce.Application.Common;

namespace ECommerce.Application.Favorites.Commands;

public record RemoveFavoriteCommand(string UserId, Guid ProductId) : IRequest<Result<bool>>;
