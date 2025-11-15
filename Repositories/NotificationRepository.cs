using Microsoft.EntityFrameworkCore;
using okem_social.Data;
using okem_social.Models;
using System.Linq;
using System.Threading.Tasks;

namespace okem_social.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _db;

        public NotificationRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Notification>> GetForUserAsync(int userId, int take = 50)
        {
            return await _db.Notifications
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _db.Notifications
                .CountAsync(x => x.UserId == userId && !x.IsRead);
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _db.Notifications.FindAsync(id);
        }

        public async Task AddAsync(Notification notification)
        {
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(int id, int userId)
        {
            var n = await _db.Notifications
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (n == null) return;

            if (!n.IsRead)
            {
                n.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var list = await _db.Notifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .ToListAsync();

            if (!list.Any()) return;

            foreach (var n in list)
            {
                n.IsRead = true;
            }

            await _db.SaveChangesAsync();
        }
    }
}
