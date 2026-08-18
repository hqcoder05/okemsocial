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
    IMediaService mediaService,
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
        var postDtos = posts.Select(post => MapToPostDtoOptimized(post, CurrentUserId)).ToList();
        return Ok(postDtos);
    }

    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PostDto>>> GetUserPosts(int userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var hasViewer = TryGetViewerId(out var viewerId);
        var posts = await postRepo.GetUserPostsAsync(userId, skip, take);
        var postDtos = posts.Select(post => MapToPostDtoOptimized(post, hasViewer ? viewerId : null)).ToList();
        return Ok(postDtos);
    }

    [HttpPost]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto dto)
    {
        var post = new Post
        {
            UserId = CurrentUserId,
            Caption = dto.Caption,
            ImageUrl = dto.ImageUrl,
            VideoUrl = dto.VideoUrl
        };

        var created = await postRepo.CreateAsync(post);

        var fullPost = await postRepo.GetByIdAsync(created.Id, includeDetails: true);
        if (fullPost == null)
            return NotFound();

        var dtoPost = MapToPostDtoOptimized(fullPost, CurrentUserId);

        // Gửi thông báo realtime cho bạn bè
        var friends = await userService.FriendsAsync(CurrentUserId);
        foreach (var friend in friends)
        {
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
        return Ok(new { message = "Cập nhật bài viết thành công." });
    }

    [HttpDelete("{postId}")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        var post = await postRepo.GetByIdAsync(postId, false);
        if (post == null)
            return NotFound();

        if (post.UserId != CurrentUserId)
            return Forbid();

        // Xóa file media trên disk nếu có
        if (!string.IsNullOrEmpty(post.ImageUrl))
            await mediaService.DeleteFileAsync(post.ImageUrl);
        if (!string.IsNullOrEmpty(post.VideoUrl))
            await mediaService.DeleteFileAsync(post.VideoUrl);

        await postRepo.DeleteAsync(postId);
        return Ok(new { message = "Đã xóa bài viết thành công." });
    }

    [HttpPost("{postId}/share")]
    public async Task<IActionResult> SharePost(int postId)
    {
        var post = await postRepo.GetByIdAsync(postId, false);
        if (post == null)
            return NotFound();

        var originalUser = await userService.GetByIdAsync(post.UserId);
        var originalName = originalUser?.FullName ?? "một người bạn";

        var newPost = new Post
        {
            UserId = CurrentUserId,
            Caption = "",
            ImageUrl = null,
            VideoUrl = null,
            OriginalPostId = post.OriginalPostId ?? post.Id,
            CreatedAt = DateTime.UtcNow
        };

        await postRepo.CreateAsync(newPost);
        return Ok(new { message = "Đã chia sẻ bài viết thành công." });
    }

    private PostDto MapToPostDtoOptimized(Post post, int? viewerId)
    {
        // Use the optimized properties from Post repository projection
        var likesCount = post.LikesCount > 0 || post.Likes == null ? post.LikesCount : post.Likes.Count;
        var commentsCount = post.CommentsCount > 0 || post.Comments == null ? post.CommentsCount : post.Comments.Count;
        var isLiked = post.IsLikedByCurrentUser;

        // Fallback for single GetByIdAsync which might still include collections
        if (post.Likes != null && post.Likes.Count > 0 && post.LikesCount == 0) {
            likesCount = post.Likes.Count;
            isLiked = viewerId.HasValue && post.Likes.Any(l => l.UserId == viewerId.Value);
        }
        if (post.Comments != null && post.Comments.Count > 0 && post.CommentsCount == 0) {
            commentsCount = post.Comments.Count;
        }

        return new PostDto
        {
            Id = post.Id,
            User = new UserDto
            {
                Id = post.User?.Id ?? post.UserId,
                Email = post.User?.Email ?? "",
                FullName = post.User?.FullName ?? "",
                Nickname = post.User?.Nickname,
                Bio = post.User?.Bio,
                AvatarUrl = post.User?.AvatarUrl,
                Role = post.User?.Role.ToString() ?? "",
                CreatedAt = post.User?.CreatedAt ?? DateTime.UtcNow
            },
            Caption = post.Caption,
            ImageUrl = post.ImageUrl,
            VideoUrl = post.VideoUrl,
            LikesCount = likesCount,
            CommentsCount = commentsCount,
            IsLikedByCurrentUser = isLiked,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            OriginalPost = post.OriginalPost != null ? new PostDto
            {
                Id = post.OriginalPost.Id,
                Caption = post.OriginalPost.Caption,
                ImageUrl = post.OriginalPost.ImageUrl,
                VideoUrl = post.OriginalPost.VideoUrl,
                CreatedAt = post.OriginalPost.CreatedAt,
                User = new UserDto
                {
                    Id = post.OriginalPost.User?.Id ?? post.OriginalPost.UserId,
                    Email = post.OriginalPost.User?.Email ?? "",
                    FullName = post.OriginalPost.User?.FullName ?? "",
                    AvatarUrl = post.OriginalPost.User?.AvatarUrl
                }
            } : null
        };
    }
}
