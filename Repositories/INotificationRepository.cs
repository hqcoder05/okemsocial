using okem_social.Models;

namespace okem_social.Repositories
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetForUserAsync(int userId, int take = 50);
        Task<int> GetUnreadCountAsync(int userId);
        Task<Notification?> GetByIdAsync(int id);

        Task AddAsync(Notification notification);
        Task MarkAsReadAsync(int id, int userId);
        Task MarkAllAsReadAsync(int userId);
    }
}
