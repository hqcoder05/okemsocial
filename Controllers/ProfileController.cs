using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.Services;

namespace okem_social.Controllers;

[Authorize]
public class ProfileController(IUserService userService) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Me()
    {
        var me = await userService.GetMeAsync(CurrentUserId);
        return View(me);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Me(string fullName)
    {
        try
        {
            await userService.UpdateProfileAsync(CurrentUserId, fullName);
            TempData["ok"] = "Đã cập nhật hồ sơ.";
        }
        catch (Exception ex)
        {
            TempData["err"] = ex.Message;
        }
        return RedirectToAction(nameof(Me));
    }
}