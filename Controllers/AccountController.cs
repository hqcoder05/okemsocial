using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.Data;
using okem_social.Services;
using okem_social.Models;

namespace okem_social.Controllers;

public class AccountController(ApplicationDbContext db, IAuthService authService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["err"] = "Vui lòng nhập email và mật khẩu.";
            return View();
        }

        // 🔁 Dùng service thay vì DbContext trực tiếp
        var user = await authService.ValidateUserAsync(email, password);
        if (user is null)
        {
            TempData["err"] = "Email hoặc mật khẩu không đúng.";
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var props     = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc   = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
        return Redirect(returnUrl ?? Url.Action("Index", "Home")!);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View();

    // (Giữ nguyên Register, vẫn dùng DbContext; có thể refactor sau)
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(string email, string fullName, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password))
        {
            TempData["err"] = "Thiếu thông tin.";
            return View();
        }
        if (password.Length < 8 || password != confirmPassword)
        {
            TempData["err"] = "Mật khẩu phải ≥ 8 ký tự và khớp xác nhận.";
            return View();
        }
        if (db.Users.Any(u => u.Email == email))
        {
            TempData["err"] = "Email đã tồn tại.";
            return View();
        }

        var user = new User
        {
            Email = email,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = Role.User
        };
        db.Users.Add(user);
        db.SaveChanges();

        TempData["ok"] = "Đăng ký thành công. Mời đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => Content("Không có quyền truy cập");
}
