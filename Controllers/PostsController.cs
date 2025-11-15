using Microsoft.AspNetCore.Mvc;
using okem_social.Repositories;
using okem_social.Services;
using okem_social.Models;

namespace okem_social.Controllers;

public class PostsController(IPostRepository postRepo) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Feed()
    {
        var currentUserId = User.Identity?.IsAuthenticated ?? false
            ? int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0")
            : 0;

        var posts = currentUserId > 0
            ? await postRepo.GetFeedAsync(currentUserId)
            : new List<Post>();

        ViewBag.CurrentUserId = currentUserId;
        return View(posts);
    }

    public IActionResult Create()
    {
        return View();
    }
}
