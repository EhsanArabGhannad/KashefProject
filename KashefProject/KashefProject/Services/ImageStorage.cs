namespace KashefProject.Services;

public sealed record ImageStorageOptions(string RootPath);

public interface IImageStorage
{
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task DeleteAsync(string publicPath);
}

public sealed class LocalImageStorage(ImageStorageOptions options) : IImageStorage
{
    private const long MaxFileSize = 8 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length is <= 0 or > MaxFileSize)
        {
            throw new InvalidOperationException("Each image must be smaller than 8 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Images must be JPG, PNG, or WebP files.");
        }

        Directory.CreateDirectory(options.RootPath);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var destination = Path.Combine(options.RootPath, fileName);
        await using var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);
        return $"/uploads/{fileName}";
    }

    public Task DeleteAsync(string publicPath)
    {
        if (!publicPath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var fileName = Path.GetFileName(publicPath);
        var fullPath = Path.GetFullPath(Path.Combine(options.RootPath, fileName));
        var root = Path.GetFullPath(options.RootPath) + Path.DirectorySeparatorChar;
        if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
