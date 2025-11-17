using Microsoft.AspNetCore.Http;

namespace ECommerce.Shared.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(IFormFile file, string subFolder, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
    Task DeleteManyAsync(IEnumerable<string> relativePaths, CancellationToken ct);
    Task<string> SaveUserAvatarAsync(Guid userId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}