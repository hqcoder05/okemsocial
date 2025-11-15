using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.Services;

namespace okem_social.Controllers;

public class ProfileController(IUserService userService) : Controller
{
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await userService.GetMeAsync(currentUserId);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(user);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Me(string fullName)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            TempData["err"] = "Họ và tên không được để trống.";
            return RedirectToAction("Me");
        }

        await userService.UpdateProfileAsync(currentUserId, fullName);
        TempData["ok"] = "Cập nhật hồ sơ thành công.";

        return RedirectToAction("Me");
    }

    public IActionResult Index()
    {
        return View();
    }
}