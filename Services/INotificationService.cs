using okem_social.DTOs;

namespace okem_social.Services
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetForUserAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);

        Task CreateAsync(CreateNotificationDto dto);
        Task MarkAsReadAsync(int id, int userId);
        Task MarkAllAsReadAsync(int userId);
    }
}
