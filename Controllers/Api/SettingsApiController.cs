using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.DTOs;
using okem_social.Services;

namespace okem_social.Controllers.Api;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsApiController(IUserService userService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lấy thông tin profile để hiển thị ở trang Settings</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        try
        {
            var user = await userService.GetMeAsync(CurrentUserId);

            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                // AvatarUrl hiện chưa lưu trong bảng User nên để null
                AvatarUrl = null,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Cập nhật tên hiển thị từ trang Settings</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await userService.UpdateProfileAsync(CurrentUserId, dto.FullName);

        // Trả lại user mới để FE cập nhật UI
        var user = await userService.GetMeAsync(CurrentUserId);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = null,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        });
    }
}
