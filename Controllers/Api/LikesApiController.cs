using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using okem_social.DTOs;
using okem_social.Hubs;
using okem_social.Repositories;
using okem_social.Services;

namespace okem_social.Controllers.Api
{
    [ApiController]
    [Route("api/likes")]
    [Authorize]
    public class LikesApiController : ControllerBase
    {
        private readonly ILikeRepository _likeRepo;
        private readonly IPostRepository _postRepo;
        private readonly IHubContext<LikeHub> _likeHub;
        private readonly INotificationService _notiService;

        public LikesApiController(
            ILikeRepository likeRepo,
            IPostRepository postRepo,
            IHubContext<LikeHub> likeHub,
            INotificationService notiService)
        {
            _likeRepo = likeRepo;
            _postRepo = postRepo;
            _likeHub = likeHub;
            _notiService = notiService;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("post/{postId:int}/toggle")]
        public async Task<IActionResult> TogglePostLike(int postId)
        {
            var userId = CurrentUserId;
            var existing = await _likeRepo.GetPostLikeAsync(postId, userId);
            bool nowLiked;

            if (existing == null)
            {
                await _likeRepo.AddPostLikeAsync(postId, userId);
                nowLiked = true;
            }
            else
            {
                await _likeRepo.RemoveAsync(existing);
                nowLiked = false;
            }

            var likesCount = await _postRepo.GetLikesCountAsync(postId);

            // 1) Realtime cho mọi client đang join group post-{postId}
            await _likeHub.Clients
                .Group(LikeHub.GetPostGroup(postId))
                .SendAsync("LikeChanged", new
                {
                    postId,
                    userId,
                    liked = nowLiked,
                    likesCount
                });

            // 2) Nếu là like mới, tạo notification cho chủ bài viết
            if (nowLiked)
            {
                // đúng tên tham số: includeDetails
                var post = await _postRepo.GetByIdAsync(postId, includeDetails: false);

                if (post != null && post.UserId != userId)
                {
                    await _notiService.CreateAsync(new CreateNotificationDto
                    {
                        UserId = post.UserId,
                        Type = "post_like",
                        Title = "Có người đã thích bài viết của bạn",
                        Content = "Ai đó vừa bấm thích bài viết của bạn.",
                        Url = $"/Posts/Feed#post-{postId}"
                    });
                }
            }

            return Ok(new { liked = nowLiked, likesCount });
        }
    }
}
