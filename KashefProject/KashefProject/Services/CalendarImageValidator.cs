namespace KashefProject.Services;

public static class CalendarImageValidator
{
    public static async Task ValidateAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is < 32 or > 8 * 1024 * 1024)
            throw new InvalidOperationException("Choose a JPG, PNG or WebP image, up to 8 MB.");
        var bytes = new byte[32];
        await using var input = file.OpenReadStream();
        if (await input.ReadAtLeastAsync(bytes, 32, false, cancellationToken) != 32)
            throw new InvalidOperationException("The image file is incomplete.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var valid = extension switch
        {
            ".jpg" or ".jpeg" => bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
            ".png" => bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
                bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8),
            ".webp" => bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
        if (!valid) throw new InvalidOperationException("The file contents must match a JPG, PNG or WebP image. SVG and other formats are not supported.");
    }
}
