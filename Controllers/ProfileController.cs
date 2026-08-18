using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.Services;

namespace okem_social.Controllers;

[Authorize]
public class ProfileController(IUserService userService, IMediaService mediaService) : Controller
{
    public async Task<IActionResult> Me()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0) return RedirectToAction("Login", "Account");

        var user = await userService.GetMeAsync(currentUserId);
        if (user == null) return RedirectToAction("Login", "Account");

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Me(string fullName)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0) return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(fullName))
        {
            TempData["err"] = "Họ và tên không được để trống.";
            return RedirectToAction("Me");
        }

        await userService.UpdateProfileAsync(currentUserId, fullName);
        TempData["ok"] = "Cập nhật hồ sơ thành công.";

        return RedirectToAction("Me");
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0) return RedirectToAction("Login", "Account");

        var user = await userService.GetMeAsync(currentUserId);
        if (user == null) return RedirectToAction("Login", "Account");

        ViewBag.AvatarUrl = user.AvatarUrl;
        ViewBag.Handle = "@" + (user.Nickname ?? user.Email.Split('@')[0]);

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string fullName, string? nickname, string? bio, string? headline, string? location, string? websiteUrl, string? coverUrl, IFormFile? avatar, IFormFile? cover)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0) return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(fullName))
        {
            TempData["err"] = "Họ và tên không được để trống.";
            return RedirectToAction(nameof(Edit));
        }

        try
        {
            var finalCoverUrl = coverUrl;
            if (cover != null && cover.Length > 0)
            {
                finalCoverUrl = await mediaService.UploadImageAsync(cover, currentUserId);
            }

            await userService.UpdateProfileAsync(currentUserId, fullName, nickname, bio, headline, location, websiteUrl, finalCoverUrl);

            if (avatar != null && avatar.Length > 0)
            {
                var avatarUrl = await mediaService.UploadImageAsync(avatar, currentUserId);
                await userService.UpdateAvatarAsync(currentUserId, avatarUrl);
            }

            TempData["ok"] = "Cập nhật thông tin thành công.";
            return RedirectToAction(nameof(Me));
        }
        catch (Exception ex)
        {
            TempData["err"] = ex.Message;
            return RedirectToAction(nameof(Edit));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAvatar()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0) return RedirectToAction("Login", "Account");

        var user = await userService.GetMeAsync(currentUserId);
        if (user.AvatarUrl != null)
        {
            await mediaService.DeleteFileAsync(user.AvatarUrl);
            await userService.UpdateAvatarAsync(currentUserId, null);
        }

        TempData["ok"] = "Đã xóa ảnh đại diện.";
        return RedirectToAction(nameof(Edit));
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return (claim != null && int.TryParse(claim.Value, out var id)) ? id : 0;
    }
}