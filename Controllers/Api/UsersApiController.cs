using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.DTOs;
using okem_social.Services;

namespace okem_social.Controllers.Api;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersApiController(
    IUserService userService,
    IMediaService mediaService) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
                AvatarUrl = user.AvatarUrl,          // ⭐
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
    {
        await userService.UpdateProfileAsync(CurrentUserId, dto.FullName);
        return Ok(new { message = "Profile updated successfully" });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await userService.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,          // ⭐
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> Search([FromQuery] string? keyword)
    {
        var users = await userService.SearchAsync(keyword ?? "", CurrentUserId);

        return Ok(users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            AvatarUrl = u.AvatarUrl,            // ⭐
            Role = u.Role.ToString(),
            CreatedAt = u.CreatedAt
        }).ToList());
    }

    [HttpPut("me/avatar")]
    public async Task<IActionResult> UpdateAvatar(IFormFile file)
    {
        try
        {
            var avatarUrl = await mediaService.UploadImageAsync(file, CurrentUserId);

            // TODO: nếu có method cập nhật avatar trong userService thì gọi ở đây
            // await userService.UpdateAvatarAsync(CurrentUserId, avatarUrl);

            return Ok(new { avatarUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Danh sách bạn bè của 1 user (2 chiều)
    /// </summary>
    [HttpGet("{id}/friends")]
    public async Task<ActionResult<List<UserDto>>> GetFriends(int id)
    {
        var friends = await userService.FriendsAsync(id);

        return Ok(friends.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            AvatarUrl = u.AvatarUrl,           // ⭐
            Role = u.Role.ToString(),
            CreatedAt = u.CreatedAt
        }).ToList());
    }

    // ====== Friend Request API ======

    /// <summary>Gửi lời mời kết bạn tới user {id}</summary>
    [HttpPost("{id}/friend-requests")]
    public async Task<IActionResult> SendFriendRequest(int id)
    {
        if (id == CurrentUserId) return BadRequest("Không thể tự gửi lời mời cho chính mình.");

        await userService.SendFriendRequestAsync(CurrentUserId, id);
        return Ok(new { message = "Đã gửi lời mời kết bạn." });
    }

    /// <summary>Chấp nhận lời mời kết bạn từ user {id}</summary>
    [HttpPost("{id}/friend-requests/accept")]
    public async Task<IActionResult> AcceptFriendRequest(int id)
    {
        if (id == CurrentUserId) return BadRequest();

        await userService.AcceptFriendRequestAsync(id, CurrentUserId);
        return Ok(new { message = "Đã chấp nhận lời mời kết bạn." });
    }

    /// <summary>Hủy lời mời kết bạn mình đã gửi tới user {id}</summary>
    [HttpDelete("{id}/friend-requests")]
    public async Task<IActionResult> CancelFriendRequest(int id)
    {
        await userService.CancelFriendRequestAsync(CurrentUserId, id);
        return Ok(new { message = "Đã hủy lời mời kết bạn." });
    }

    /// <summary>Hủy kết bạn với user {id}</summary>
    [HttpDelete("{id}/friends")]
    public async Task<IActionResult> RemoveFriend(int id)
    {
        await userService.RemoveFriendAsync(CurrentUserId, id);
        return Ok(new { message = "Đã hủy kết bạn." });
    }

    /// <summary>Lời mời kết bạn đến (người khác gửi cho mình)</summary>
    [HttpGet("me/friend-requests/incoming")]
    public async Task<ActionResult<List<UserDto>>> GetIncomingRequests()
    {
        var users = await userService.IncomingRequestsAsync(CurrentUserId);

        return Ok(users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            AvatarUrl = u.AvatarUrl,           // ⭐
            Role = u.Role.ToString(),
            CreatedAt = u.CreatedAt
        }).ToList());
    }

    /// <summary>Lời mời kết bạn đã gửi (mình gửi cho người khác)</summary>
    [HttpGet("me/friend-requests/outgoing")]
    public async Task<ActionResult<List<UserDto>>> GetOutgoingRequests()
    {
        var users = await userService.OutgoingRequestsAsync(CurrentUserId);

        return Ok(users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            AvatarUrl = u.AvatarUrl,           // ⭐
            Role = u.Role.ToString(),
            CreatedAt = u.CreatedAt
        }).ToList());
    }
}
