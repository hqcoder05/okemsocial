using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using okem_social.DTOs;
using okem_social.Hubs;
using okem_social.Models;
using okem_social.Repositories;
using okem_social.Services;

namespace okem_social.Controllers.Api
{
    [ApiController]
    [Route("api/comments")]
    [Authorize]
    public class CommentsApiController : ControllerBase
    {
        private readonly ICommentRepository _commentRepo;
        private readonly IPostRepository _postRepo;
        private readonly IHubContext<CommentHub> _commentHub;
        private readonly INotificationService _notiService;

        public CommentsApiController(
            ICommentRepository commentRepo,
            IPostRepository postRepo,
            IHubContext<CommentHub> commentHub,
            INotificationService notiService)
        {
            _commentRepo = commentRepo;
            _postRepo = postRepo;
            _commentHub = commentHub;
            _notiService = notiService;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: /api/comments/post/5
        [HttpGet("post/{postId:int}")]
        public async Task<ActionResult<List<CommentDto>>> GetForPost(int postId)
        {
            // Có thể check post tồn tại hay không nếu muốn
            var list = await _commentRepo.GetByPostIdAsync(postId);

            var dtos = list.Select(c => new CommentDto
            {
                Id = c.Id,
                User = new UserDto
                {
                    Id = c.User!.Id,
                    Email = c.User.Email,
                    FullName = c.User.FullName,
                    AvatarUrl = c.User.AvatarUrl,
                    Role = c.User.Role.ToString(),
                    CreatedAt = c.User.CreatedAt
                },
                Content = c.Content,
                LikesCount = 0,           // nếu có like comment thì map thêm
                IsLikedByCurrentUser = false,
                CreatedAt = c.CreatedAt
            }).ToList();

            return Ok(dtos);
        }

        // POST: /api/comments/post/5
        [HttpPost("post/{postId:int}")]
        public async Task<ActionResult<CommentDto>> Create(int postId, [FromBody] CreateCommentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { message = "Nội dung bình luận không được để trống." });
            }

            // đảm bảo post tồn tại
            var post = await _postRepo.GetByIdAsync(postId);
            if (post == null)
            {
                return NotFound(new { message = "Bài viết không tồn tại." });
            }

            var comment = new Comment
            {
                PostId = postId,
                UserId = CurrentUserId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _commentRepo.CreateAsync(comment);

            // reload để có User navigation
            var full = await _commentRepo.GetByIdAsync(created.Id);
            if (full == null)
            {
                return NotFound();
            }

            var dtoResult = new CommentDto
            {
                Id = full.Id,
                User = new UserDto
                {
                    Id = full.User!.Id,
                    Email = full.User.Email,
                    FullName = full.User.FullName,
                    AvatarUrl = full.User.AvatarUrl,
                    Role = full.User.Role.ToString(),
                    CreatedAt = full.User.CreatedAt
                },
                Content = full.Content,
                LikesCount = 0,
                IsLikedByCurrentUser = false,
                CreatedAt = full.CreatedAt
            };

            // 1) Realtime cho group post-{postId}
            await _commentHub.Clients
                .Group(CommentHub.GetPostGroup(postId))
                .SendAsync("CommentAdded", new
                {
                    postId,
                    commentId = dtoResult.Id,
                    user = new
                    {
                        id = dtoResult.User.Id,
                        fullName = dtoResult.User.FullName,
                        email = dtoResult.User.Email,
                        avatarUrl = dtoResult.User.AvatarUrl
                    },
                    content = dtoResult.Content,
                    createdAt = dtoResult.CreatedAt
                });

            // 2) Notification cho chủ bài viết (nếu không phải chính mình)
            if (post.UserId != CurrentUserId)
            {
                await _notiService.CreateAsync(new CreateNotificationDto
                {
                    UserId = post.UserId,
                    Type = "post_comment",
                    Title = "Có bình luận mới trên bài viết của bạn",
                    Content = dto.Content,
                    Url = $"/Posts/Feed#post-{postId}"
                });
            }

            return Ok(dtoResult);
        }
    }
}
