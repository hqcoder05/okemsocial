using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.Services;

namespace okem_social.Controllers.Api
{
    [ApiController]
    [Route("api/media")]
    [Authorize]
    public class MediaApiController : ControllerBase
    {
        private readonly IMediaService _mediaService;

        public MediaApiController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Không có file nào được gửi lên." });
            }

            var userId = CurrentUserId;
            if (userId <= 0)
            {
                return Unauthorized();
            }

            // Ảnh
            if (_mediaService.IsValidImage(file))
            {
                var url = await _mediaService.UploadImageAsync(file, userId);
                return Ok(new
                {
                    url,
                    type = "image"
                });
            }

            // Video
            if (_mediaService.IsValidVideo(file))
            {
                var url = await _mediaService.UploadVideoAsync(file, userId);
                return Ok(new
                {
                    url,
                    type = "video"
                });
            }

            return BadRequest(new { message = "Định dạng hoặc kích thước file không hợp lệ." });
        }
    }
}
