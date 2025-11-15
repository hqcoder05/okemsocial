using okem_social.Data;
using okem_social.Models;

namespace okem_social.Services;

public class MediaService : IMediaService
{
    private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly string[] _allowedVideoExtensions = { ".mp4", ".mov", ".avi", ".webm" };
    private const long MaxImageSize = 10 * 1024 * 1024;   // 10MB
    private const long MaxVideoSize = 100 * 1024 * 1024;  // 100MB

    private readonly IWebHostEnvironment _env;
    private readonly string _webRoot;

    public MediaService(IWebHostEnvironment env)
    {
        _env = env;
        _webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }

    public bool IsValidImage(IFormFile file)
    {
        if (file == null || file.Length == 0) return false;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedImageExtensions.Contains(extension) && file.Length <= MaxImageSize;
    }

    public bool IsValidVideo(IFormFile file)
    {
        if (file == null || file.Length == 0) return false;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedVideoExtensions.Contains(extension) && file.Length <= MaxVideoSize;
    }

    public async Task<string> UploadImageAsync(IFormFile file, int userId)
    {
        if (!IsValidImage(file))
            throw new ArgumentException("File ảnh không hợp lệ.", nameof(file));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var folder = Path.Combine(_webRoot, "uploads", "images");
        Directory.CreateDirectory(folder);

        var fileName = $"u{userId}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(folder, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        // URL public
        return $"/uploads/images/{fileName}";
    }

    public async Task<string> UploadVideoAsync(IFormFile file, int userId)
    {
        if (!IsValidVideo(file))
            throw new ArgumentException("File video không hợp lệ.", nameof(file));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var folder = Path.Combine(_webRoot, "uploads", "videos");
        Directory.CreateDirectory(folder);

        var fileName = $"u{userId}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(folder, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/videos/{fileName}";
    }

    public async Task<bool> DeleteFileAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        // url dạng /uploads/images/abc.jpg hoặc /uploads/videos/xyz.mp4
        var relativePath = url.TrimStart('/', '\\');
        var fullPath = Path.Combine(_webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath)) return false;

        try
        {
            await Task.Run(() => File.Delete(fullPath));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
