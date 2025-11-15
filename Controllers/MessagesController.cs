using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.Models;
using okem_social.Repositories;
using okem_social.Services;

namespace okem_social.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IUserService _userService;

    public MessagesController(
        IConversationRepository conversationRepo,
        IUserService userService)
    {
        _conversationRepo = conversationRepo;
        _userService = userService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private bool IsAjaxRequest()
    {
        // fetch(...) ở view sẽ gửi header này
        return string.Equals(
            Request.Headers["X-Requested-With"].ToString(),
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }

    // Trang danh sách tin nhắn
    public async Task<IActionResult> Index()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Redirect("/Account/Login?returnUrl=/Messages");
        }

        var currentUserId = CurrentUserId;

        // Các cuộc trò chuyện hiện có
        var conversations = await _conversationRepo.GetUserConversationsAsync(currentUserId);

        // Danh sách bạn bè đã kết bạn
        var friends = await _userService.FriendsAsync(currentUserId);

        ViewBag.CurrentUserId = currentUserId;
        ViewBag.Friends = friends;

        return View(conversations);
    }

    // Lấy UI chat của 1 conversation (dùng AJAX)
    [HttpGet]
    public async Task<IActionResult> Chat(int id)
    {
        var currentUserId = CurrentUserId;
        if (currentUserId == 0)
        {
            return Unauthorized();
        }

        // Chỉ cho vào nếu mình là member
        if (!await _conversationRepo.IsMemberAsync(id, currentUserId))
            return Forbid();

        var conversation = await _conversationRepo.GetByIdAsync(id, includeMembers: true);
        if (conversation == null) return NotFound();

        ViewBag.CurrentUserId = currentUserId;

        // Nếu là AJAX thì trả về partial để nhét vào khung bên phải
        if (IsAjaxRequest())
        {
            return PartialView("Chat", conversation);
        }

        // Nếu người dùng gõ trực tiếp URL /Messages/Chat/1
        // -> quay về Index và để client tự xử lý
        return RedirectToAction(nameof(Index), new { openConversationId = id });
    }

    // Bấm vào 1 người bạn -> tìm / tạo đoạn chat 2 người (dùng AJAX)
    [HttpGet]
    public async Task<IActionResult> ChatWith(int friendId)
    {
        var currentUserId = CurrentUserId;
        if (currentUserId == 0)
        {
            return Unauthorized();
        }

        if (friendId == currentUserId)
            return BadRequest("Không thể chat với chính mình.");

        // Kiểm tra 2 người có phải bạn bè không
        var areFriends = await _userService.AreFriendsAsync(currentUserId, friendId);
        if (!areFriends)
            return Forbid();

        // Tìm hoặc tạo conversation 2 người
        var conversation =
            await _conversationRepo.GetOrCreateDirectConversationAsync(currentUserId, friendId);

        ViewBag.CurrentUserId = currentUserId;

        if (IsAjaxRequest())
        {
            // Trả về giao diện chat để nhét vào khung bên phải
            return PartialView("Chat", conversation);
        }

        // Trường hợp user gõ URL trực tiếp
        return RedirectToAction(nameof(Index), new { openConversationId = conversation.Id });
    }
}
