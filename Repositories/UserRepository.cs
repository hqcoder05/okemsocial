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

    // ---- Follow ----
    public Task<bool> IsFollowingAsync(int followerId, int followeeId) =>
        db.Follows.AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);

    public async Task AddFollowAsync(int followerId, int followeeId)
    {
        if (followerId == followeeId) return;

        var exists = await db.Follows.FindAsync(followerId, followeeId);
        if (exists == null)
        {
            db.Follows.Add(new Follow
            {
                FollowerId = followerId,
                FolloweeId = followeeId
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveFollowAsync(int followerId, int followeeId)
    {
        var exists = await db.Follows.FindAsync(followerId, followeeId);
        if (exists != null)
        {
            db.Follows.Remove(exists);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<User>> GetFollowersAsync(int userId) =>
        await db.Follows
            .Where(f => f.FolloweeId == userId)
            .Include(f => f.Follower)
            .Select(f => f.Follower!)
            .OrderBy(u => u.FullName)
            .ToListAsync();

    public async Task<List<User>> GetFollowingAsync(int userId) =>
        await db.Follows
            .Where(f => f.FollowerId == userId)
            .Include(f => f.Followee)
            .Select(f => f.Followee!)
            .OrderBy(u => u.FullName)
            .ToListAsync();
}
