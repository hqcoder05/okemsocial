using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.Services;

namespace okem_social.Controllers;

[Authorize]
public class UsersController(IUserService userService) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var u = await userService.GetByIdAsync(id);
        if (u == null) return NotFound();

        var isAuth = User.Identity?.IsAuthenticated == true;
        ViewBag.IsMe = isAuth && id == CurrentUserId;
        ViewBag.IsFollowing = isAuth && !ViewBag.IsMe
            ? await userService.IsFollowingAsync(CurrentUserId, id)
            : false;

        return View(u);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? keyword)
    {
        var results = await userService.SearchAsync(keyword ?? "", CurrentUserId);
        ViewBag.Keyword = keyword ?? "";
        return View(results);
    }

    [HttpGet]
    public async Task<IActionResult> Followers(int id)
    {
        var target = await userService.GetByIdAsync(id);
        if (target == null) return NotFound();

        ViewBag.Target = target;
        var followers = await userService.FollowersAsync(id);
        return View(followers);
    }

    [HttpGet]
    public async Task<IActionResult> Following(int id)
    {
        var target = await userService.GetByIdAsync(id);
        if (target == null) return NotFound();

        ViewBag.Target = target;
        var following = await userService.FollowingAsync(id);
        return View(following);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(int id)
    {
        await userService.FollowAsync(CurrentUserId, id);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfollow(int id)
    {
        await userService.UnfollowAsync(CurrentUserId, id);
        return RedirectToAction(nameof(Details), new { id });
    }
}
