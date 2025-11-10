using ECommerce.Shared.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Infrastructure.Files;

public class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorage(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveAsync(IFormFile file, string subFolder, CancellationToken ct)
    {
        var wwwroot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var folder = Path.Combine(wwwroot, subFolder);
        Directory.CreateDirectory(folder);

        var safeName = Path.GetFileNameWithoutExtension(file.FileName);
        var ext = Path.GetExtension(file.FileName);
        var unique = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, unique);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        // Return relative path used by static files middleware
        return Path.Combine(subFolder, unique).Replace("\\", "/");
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return Task.CompletedTask;

        var root = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var fullPath = Path.Combine(root, relativePath.TrimStart('/', '\\'));

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public async Task DeleteManyAsync(IEnumerable<string> relativePaths, CancellationToken ct)
    {
        foreach (var path in relativePaths)
            await DeleteAsync(path, ct);
    }
}