using Microsoft.AspNetCore.Http;

namespace okem_social.Services
{
    public interface IMediaService
    {
        // Validate
        bool IsValidImage(IFormFile file);
        bool IsValidVideo(IFormFile file);

        // Upload
        Task<string> UploadImageAsync(IFormFile file, int userId);
        Task<string> UploadVideoAsync(IFormFile file, int userId);

        // Delete (nếu cần xoá file cũ khi sửa bài)
        Task<bool> DeleteFileAsync(string url);
    }
}
