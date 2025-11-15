using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using okem_social.Repositories;
using okem_social.Models;

namespace okem_social.Controllers;

[Authorize]
public class UsersController(IUserRepository userRepo) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await userRepo.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        ViewBag.IsMe = currentUserId == user.Id;

        bool isFriend = false;
        bool hasSentRequest = false;
        bool hasIncomingRequest = false;

        if (currentUserId != 0 && currentUserId != user.Id)
        {
            isFriend = await userRepo.AreFriendsAsync(currentUserId, user.Id);
            hasSentRequest = await userRepo.HasPendingRequestAsync(currentUserId, user.Id);
            hasIncomingRequest = await userRepo.HasIncomingRequestAsync(currentUserId, user.Id);
        }

        ViewBag.IsFriend = isFriend;
        ViewBag.HasSentRequest = hasSentRequest;
        ViewBag.HasIncomingRequest = hasIncomingRequest;

        var friends = await userRepo.GetFriendsAsync(user.Id);
        ViewBag.FriendsCount = friends.Count;

        return View(user);
    }

    public async Task<IActionResult> Search(string keyword = "")
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (currentUserId == 0)
        {
            return Redirect("/Account/Login?returnUrl=/Users/Search");
        }

        try
        {
            var users = await userRepo.SearchAsync(keyword, currentUserId) ?? new List<User>();
            var userViewModels = new List<UserSearchViewModel>();

            foreach (var user in users)
            {
                var isFriend = await userRepo.AreFriendsAsync(currentUserId, user.Id);
                var hasSentRequest = await userRepo.HasPendingRequestAsync(currentUserId, user.Id);
                var hasIncomingRequest = await userRepo.HasIncomingRequestAsync(currentUserId, user.Id);

                userViewModels.Add(new UserSearchViewModel
                {
                    User = user,
                    IsFriend = isFriend,
                    HasSentRequest = hasSentRequest,
                    HasIncomingRequest = hasIncomingRequest,
                    IsCurrentUser = user.Id == currentUserId
                });
            }

            ViewBag.Keyword = keyword;
            ViewBag.CurrentUserId = currentUserId; // ✅ để view Search dùng tạo link xem lời mời

            return View(userViewModels);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Search error: {ex.Message}");
            ViewBag.Keyword = keyword;
            ViewBag.CurrentUserId = currentUserId;
            return View(new List<UserSearchViewModel>());
        }
    }

    // Danh sách lời mời kết bạn ĐẾN (incoming)
    public async Task<IActionResult> IncomingRequests(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == 0) return Unauthorized();
        if (currentUserId != id) return Forbid();

        var target = await userRepo.GetByIdAsync(id);
        if (target == null) return NotFound();

        var users = await userRepo.GetIncomingRequestsAsync(id);
        ViewBag.Target = target;
        return View("IncomingRequests", users);
    }

    // Danh sách lời mời kết bạn ĐÃ GỬI (outgoing)
    public async Task<IActionResult> OutgoingRequests(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == 0) return Unauthorized();
        if (currentUserId != id) return Forbid();

        var target = await userRepo.GetByIdAsync(id);
        if (target == null) return NotFound();

        var users = await userRepo.GetOutgoingRequestsAsync(id);
        ViewBag.Target = target;
        return View("OutgoingRequests", users);
    }

    // Gửi lời mời kết bạn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendRequest(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == 0) return Unauthorized();
        if (currentUserId == id) return BadRequest();

        await userRepo.SendFriendRequestAsync(currentUserId, id);
        return RedirectToAction("Details", new { id });
    }

    // Chấp nhận lời mời (id = người đã gửi lời mời)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == 0) return Unauthorized();
        if (currentUserId == id) return BadRequest();

        await userRepo.AcceptFriendRequestAsync(id, currentUserId);
        return RedirectToAction("Details", new { id });
    }

    // Hủy lời mời mình đã gửi
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == 0) return Unauthorized();

        await userRepo.CancelFriendRequestAsync(currentUserId, id);
        return RedirectToAction("Details", new { id });
    }

    // Hủy kết bạn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfriend(int id)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == 0) return Unauthorized();

        await userRepo.RemoveFriendAsync(currentUserId, id);
        return RedirectToAction("Details", new { id });
    }
}
