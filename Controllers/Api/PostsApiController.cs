using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using okem_social.DTOs;
using okem_social.Hubs;
using okem_social.Models;
using okem_social.Repositories;
using okem_social.Services;

namespace okem_social.Controllers.Api;

[ApiController]
[Route("api/posts")]
[Authorize]
public class PostsApiController(
    IPostRepository postRepo,
    IUserService userService,
    INotificationRepository notiRepo,
    IHubContext<NotificationHub> notiHub) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool TryGetViewerId(out int viewerId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out viewerId);

    [HttpGet("feed")]
    public async Task<ActionResult<List<PostDto>>> GetFeed([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var posts = await postRepo.GetFeedAsync(CurrentUserId, skip, take);
        var postDtos = new List<PostDto>();

        foreach (var post in posts)
        {
            postDtos.Add(await MapToPostDto(post));
        }

        return Ok(postDtos);
    }

    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PostDto>>> GetUserPosts(int userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var posts = await postRepo.GetUserPostsAsync(userId, skip, take);
        var postDtos = new List<PostDto>();

        foreach (var post in posts)
        {
            postDtos.Add(await MapToPostDto(post));
        }

        return Ok(postDtos);
    }

    [HttpPost]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto dto)
    {
        var post = new Post
        {
            UserId = CurrentUserId,
            Caption = dto.Caption,      // quote
            ImageUrl = dto.ImageUrl,    // ảnh (nếu có)
            VideoUrl = dto.VideoUrl     // video (nếu có)
        };

        var created = await postRepo.CreateAsync(post);

        // Reload với User
        var fullPost = await postRepo.GetByIdAsync(created.Id, includeDetails: false);
        if (fullPost == null)
            return NotFound();

        var dtoPost = await MapToPostDto(fullPost);

        // ===== GỬI THÔNG BÁO REALTIME CHO BẠN BÈ =====
        var friends = await userService.FriendsAsync(CurrentUserId);
        foreach (var friend in friends)
        {
            // Không gửi cho chính mình
            if (friend.Id == CurrentUserId) continue;

            var noti = new Notification
            {
                UserId = friend.Id,
                Type = "friend_post",
                Title = "Bạn bè vừa đăng bài mới",
                Content = $"{fullPost.User!.FullName} vừa đăng một bài viết mới.",
                Url = $"/Posts/Feed#post-{fullPost.Id}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await notiRepo.AddAsync(noti);

            await notiHub.Clients
                .Group(NotificationHub.GetUserGroup(friend.Id))
                .SendAsync("NotificationReceived", new
                {
                    id = noti.Id,
                    noti.Type,
                    noti.Title,
                    noti.Content,
                    noti.Url,
                    noti.IsRead,
                    noti.CreatedAt
                });
        }

        return Ok(dtoPost);
    }

    [HttpPut("{postId}")]
    public async Task<IActionResult> UpdatePost(int postId, [FromBody] UpdatePostDto dto)
    {
        var post = await postRepo.GetByIdAsync(postId, false);
        if (post == null)
            return NotFound();

        if (post.UserId != CurrentUserId)
            return Forbid();

        post.Caption = dto.Caption;
        post.UpdatedAt = DateTime.UtcNow;

        await postRepo.UpdateAsync(post);
        return Ok(new { message = "Post updated successfully" });
    }

    [HttpDelete("{postId}")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        var post = await postRepo.GetByIdAsync(postId, false);
        if (post == null)
            return NotFound();

        if (post.UserId != CurrentUserId)
            return Forbid();

        await postRepo.DeleteAsync(postId);
        return Ok(new { message = "Post deleted successfully" });
    }

    private async Task<PostDto> MapToPostDto(Post post)
    {
        var hasViewer = TryGetViewerId(out var viewerId);

        return new PostDto
        {
            Id = post.Id,
            User = new UserDto
            {
                Id = post.User!.Id,
                Email = post.User.Email,
                FullName = post.User.FullName,
                AvatarUrl = post.User.AvatarUrl,   // ⭐ thêm avatar
                Role = post.User.Role.ToString(),
                CreatedAt = post.User.CreatedAt
            },
            Caption = post.Caption,
            ImageUrl = post.ImageUrl,
            VideoUrl = post.VideoUrl,
            LikesCount = await postRepo.GetLikesCountAsync(post.Id),
            CommentsCount = await postRepo.GetCommentsCountAsync(post.Id),
            IsLikedByCurrentUser = hasViewer
                ? await postRepo.IsLikedByUserAsync(post.Id, viewerId)
                : false,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
    }
}
