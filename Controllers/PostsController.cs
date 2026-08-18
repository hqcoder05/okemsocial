using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using okem_social.Data;
using okem_social.Repositories;
using okem_social.Services;
using okem_social.Models;

namespace okem_social.Controllers;

[Authorize]
public class PostsController(IPostRepository postRepo, ApplicationDbContext db) : Controller
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

        // Load REAL contacts from SQL Server database
        var friends = await db.FriendRequests
            .Where(fr => fr.IsAccepted && (fr.FromUserId == currentUserId || fr.ToUserId == currentUserId))
            .Select(fr => (fr.FromUserId == currentUserId ? fr.ToUser : fr.FromUser)!)
            .Distinct()
            .ToListAsync();

        // Supplement with other active community members
        var existingIds = friends.Where(f => f != null).Select(f => f.Id).Append(currentUserId).ToList();
        var suggestions = await db.Users
            .Where(u => !existingIds.Contains(u.Id))
            .Take(5)
            .ToListAsync();

        ViewBag.CurrentUserId = currentUserId;
        ViewBag.Contacts = friends.Count > 0 ? friends : suggestions;
        ViewBag.SuggestedUsers = suggestions;
        ViewBag.TrendingTopics = await postRepo.GetTrendingTopicsAsync(5);
        return View(posts);
    }

    public IActionResult Create()
    {
        return View();
    }
}
