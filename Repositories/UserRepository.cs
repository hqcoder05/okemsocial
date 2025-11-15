using Microsoft.EntityFrameworkCore;
using okem_social.Data;
using okem_social.Models;

namespace okem_social.Repositories;

public class UserRepository(ApplicationDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(int id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task UpdateAsync(User user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    public async Task<List<User>> SearchAsync(string keyword, int excludeUserId, int take = 50)
    {
        keyword = (keyword ?? string.Empty).Trim();
        var q = db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
            q = q.Where(u => u.FullName.Contains(keyword) || u.Email.Contains(keyword));

        return await q.Where(u => u.Id != excludeUserId)
                      .OrderBy(u => u.FullName)
                      .Take(take)
                      .ToListAsync();
    }

    // ---- Friend / Kết bạn ----

    public async Task SendFriendRequestAsync(int fromId, int toId)
    {
        if (fromId == toId) return;

        // Nếu đã là bạn rồi thì không gửi nữa
        if (await AreFriendsAsync(fromId, toId)) return;

        var exists = await db.FriendRequests.FindAsync(fromId, toId);
        if (exists == null)
        {
            db.FriendRequests.Add(new FriendRequest
            {
                FromUserId = fromId,
                ToUserId = toId
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task AcceptFriendRequestAsync(int fromId, int toId)
    {
        var request = await db.FriendRequests.FindAsync(fromId, toId);
        if (request != null && !request.IsAccepted)
        {
            request.IsAccepted = true;
            await db.SaveChangesAsync();
        }
    }

    public async Task CancelFriendRequestAsync(int fromId, int toId)
    {
        var exists = await db.FriendRequests.FindAsync(fromId, toId);
        if (exists != null && !exists.IsAccepted)
        {
            db.FriendRequests.Remove(exists);
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveFriendAsync(int userId, int otherId)
    {
        var request = await db.FriendRequests.FirstOrDefaultAsync(fr =>
            fr.IsAccepted &&
            ((fr.FromUserId == userId && fr.ToUserId == otherId) ||
             (fr.FromUserId == otherId && fr.ToUserId == userId)));

        if (request != null)
        {
            db.FriendRequests.Remove(request);
            await db.SaveChangesAsync();
        }
    }

    public Task<bool> AreFriendsAsync(int userId, int otherId) =>
        db.FriendRequests.AnyAsync(fr =>
            fr.IsAccepted &&
            ((fr.FromUserId == userId && fr.ToUserId == otherId) ||
             (fr.FromUserId == otherId && fr.ToUserId == userId)));

    public Task<bool> HasPendingRequestAsync(int fromId, int toId) =>
        db.FriendRequests.AnyAsync(fr =>
            !fr.IsAccepted && fr.FromUserId == fromId && fr.ToUserId == toId);

    public Task<bool> HasIncomingRequestAsync(int currentUserId, int otherId) =>
        db.FriendRequests.AnyAsync(fr =>
            !fr.IsAccepted && fr.FromUserId == otherId && fr.ToUserId == currentUserId);

    public async Task<List<User>> GetFriendsAsync(int userId)
    {
        var friendRequests = await db.FriendRequests
            .Include(fr => fr.FromUser)
            .Include(fr => fr.ToUser)
            .Where(fr => fr.IsAccepted &&
                        (fr.FromUserId == userId || fr.ToUserId == userId))
            .ToListAsync();

        return friendRequests
            .Select(fr => fr.FromUserId == userId ? fr.ToUser! : fr.FromUser!)
            .OrderBy(u => u.FullName)
            .ToList();
    }

    public async Task<List<User>> GetIncomingRequestsAsync(int userId)
    {
        return await db.FriendRequests
            .Include(fr => fr.FromUser)
            .Where(fr => !fr.IsAccepted && fr.ToUserId == userId)
            .Select(fr => fr.FromUser!)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<List<User>> GetOutgoingRequestsAsync(int userId)
    {
        return await db.FriendRequests
            .Include(fr => fr.ToUser)
            .Where(fr => !fr.IsAccepted && fr.FromUserId == userId)
            .Select(fr => fr.ToUser!)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }
}
