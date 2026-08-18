using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using okem_social.DTOs;
using okem_social.Services;

namespace okem_social.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    [Route("api/[controller]")]
    public class NotificationsApiController : ControllerBase
    {
        private readonly INotificationService _service;
        private readonly ILogger<NotificationsApiController> _logger;

        public NotificationsApiController(
            INotificationService service,
            ILogger<NotificationsApiController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        // GET: /api/notifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetMyNotifications()
        {
            var userId = CurrentUserId;
            if (userId == 0) return Unauthorized();

            var list = await _service.GetForUserAsync(userId);
            return Ok(list);
        }

        // GET: /api/notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = CurrentUserId;
            if (userId == 0) return Unauthorized();

            var count = await _service.GetUnreadCountAsync(userId);
            return Ok(count);
        }

        // POST: /api/notifications/{id}/read
        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = CurrentUserId;
            if (userId == 0) return Unauthorized();

            await _service.MarkAsReadAsync(id, userId);
            return NoContent();
        }

        // POST: /api/notifications/read-all
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = CurrentUserId;
            if (userId == 0) return Unauthorized();

            await _service.MarkAllAsReadAsync(userId);
            return NoContent();
        }
    }
}
