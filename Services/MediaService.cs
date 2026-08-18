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
        if (!_allowedImageExtensions.Contains(extension) || file.Length > MaxImageSize) return false;

        // Check magic bytes for Image (JPEG, PNG, GIF, WEBP)
        using var stream = file.OpenReadStream();
        var header = new byte[12];
        if (stream.Read(header, 0, 12) < 4) return false;

        bool isJpeg = header[0] == 0xFF && header[1] == 0xD8;
        bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        bool isGif = header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46;
        bool isWebp = header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50; // 'WEBP'

        return isJpeg || isPng || isGif || isWebp;
    }

    public bool IsValidVideo(IFormFile file)
    {
        if (file == null || file.Length == 0) return false;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedVideoExtensions.Contains(extension) || file.Length > MaxVideoSize) return false;

        // Check magic bytes for Video (MP4)
        using var stream = file.OpenReadStream();
        var header = new byte[12];
        if (stream.Read(header, 0, 12) < 12) return false;

        // FTYP box for MP4
        bool isMp4 = header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70; // 'ftyp'
        
        return isMp4;
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

        // Chỉ trích xuất tên file, loại bỏ toàn bộ chuỗi đường dẫn (../) để chống Path Traversal
        var fileName = Path.GetFileName(new Uri(url, UriKind.RelativeOrAbsolute).LocalPath);
        if (string.IsNullOrEmpty(fileName)) return false;

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var isVideo = _allowedVideoExtensions.Contains(ext);
        var subFolder = isVideo ? "videos" : "images";

        var fullPath = Path.Combine(_webRoot, "uploads", subFolder, fileName);

        if (!File.Exists(fullPath)) return false;

        try
        {
            File.Delete(fullPath);
            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }
}
