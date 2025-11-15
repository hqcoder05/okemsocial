using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.Repositories;

namespace okem_social.Controllers.Api;

[ApiController]
[Route("api/friends")]
[Authorize]
public class FriendsApiController : ControllerBase
{
    private readonly IUserRepository _userRepo;

    public FriendsApiController(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Gửi lời mời kết bạn (CurrentUser -> targetUserId)
    /// </summary>
    [HttpPost("{targetUserId}")]
    public async Task<IActionResult> SendRequest(int targetUserId)
    {
        if (targetUserId == CurrentUserId)
            return BadRequest(new { message = "Không thể tự kết bạn với chính mình." });

        var targetUser = await _userRepo.GetByIdAsync(targetUserId);
        if (targetUser == null)
            return NotFound(new { message = "User không tồn tại." });

        await _userRepo.SendFriendRequestAsync(CurrentUserId, targetUserId);
        return Ok(new { message = "Đã gửi lời mời kết bạn." });
    }

    /// <summary>
    /// Chấp nhận lời mời kết bạn (fromUserId -> CurrentUser)
    /// </summary>
    [HttpPost("{fromUserId}/accept")]
    public async Task<IActionResult> Accept(int fromUserId)
    {
        if (fromUserId == CurrentUserId)
            return BadRequest(new { message = "Không hợp lệ." });

        var fromUser = await _userRepo.GetByIdAsync(fromUserId);
        if (fromUser == null)
            return NotFound(new { message = "User không tồn tại." });

        await _userRepo.AcceptFriendRequestAsync(fromUserId, CurrentUserId);
        return Ok(new { message = "Đã chấp nhận lời mời kết bạn." });
    }

    /// <summary>
    /// Hủy lời mời mình đã gửi (CurrentUser -> targetUserId) khi còn pending
    /// </summary>
    [HttpDelete("{targetUserId}/request")]
    public async Task<IActionResult> CancelRequest(int targetUserId)
    {
        if (targetUserId == CurrentUserId)
            return BadRequest(new { message = "Không hợp lệ." });

        await _userRepo.CancelFriendRequestAsync(CurrentUserId, targetUserId);
        return Ok(new { message = "Đã hủy lời mời kết bạn." });
    }

    /// <summary>
    /// Hủy kết bạn (Unfriend) giữa CurrentUser và targetUserId
    /// </summary>
    [HttpDelete("{targetUserId}")]
    public async Task<IActionResult> Unfriend(int targetUserId)
    {
        if (targetUserId == CurrentUserId)
            return BadRequest(new { message = "Không hợp lệ." });

        await _userRepo.RemoveFriendAsync(CurrentUserId, targetUserId);
        return Ok(new { message = "Đã hủy kết bạn." });
    }
}
