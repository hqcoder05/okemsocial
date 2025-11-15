using Microsoft.AspNetCore.SignalR;
using okem_social.DTOs;
using okem_social.Hubs;
using okem_social.Models;
using okem_social.Repositories;

namespace okem_social.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IHubContext<NotificationHub> _notiHub;

        public NotificationService(
            INotificationRepository repo,
            IHubContext<NotificationHub> notiHub)
        {
            _repo = repo;
            _notiHub = notiHub;
        }

        public async Task<List<NotificationDto>> GetForUserAsync(int userId)
        {
            var list = await _repo.GetForUserAsync(userId);

            return list.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Content = n.Content ?? string.Empty,
                Url = n.Url,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();
        }

        public Task<int> GetUnreadCountAsync(int userId)
        {
            return _repo.GetUnreadCountAsync(userId);
        }

        /// <summary>
        /// Tạo notification + lưu DB + bắn realtime qua NotificationHub
        /// </summary>
        public async Task CreateAsync(CreateNotificationDto dto)
        {
            var entity = new Notification
            {
                UserId = dto.UserId,
                Type = dto.Type,
                Title = dto.Title,
                Content = dto.Content,
                Url = dto.Url,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entity);

            var payload = new NotificationDto
            {
                Id = entity.Id,
                Type = entity.Type,
                Title = entity.Title,
                Content = entity.Content ?? string.Empty,
                Url = entity.Url,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };

            // gửi realtime cho đúng user
            await _notiHub.Clients
                .Group(NotificationHub.GetUserGroup(dto.UserId))
                .SendAsync("NotificationReceived", payload);
        }

        public Task MarkAsReadAsync(int id, int userId)
        {
            return _repo.MarkAsReadAsync(id, userId);
        }

        public Task MarkAllAsReadAsync(int userId)
        {
            return _repo.MarkAllAsReadAsync(userId);
        }
    }
}
