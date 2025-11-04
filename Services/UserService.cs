using okem_social.Models;
using okem_social.Repositories;

namespace okem_social.Services;

public class UserService(IUserRepository repo) : IUserService
{
    public async Task<User> GetMeAsync(int currentUserId) =>
        await repo.GetByIdAsync(currentUserId) ?? throw new KeyNotFoundException("User không tồn tại.");

    public async Task UpdateProfileAsync(int currentUserId, string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("Họ tên không được rỗng.");

        var me = await GetMeAsync(currentUserId);
        me.FullName = fullName.Trim();
        await repo.UpdateAsync(me);
    }

    public Task<User?> GetByIdAsync(int id) => repo.GetByIdAsync(id);

    public Task<List<User>> SearchAsync(string keyword, int currentUserId) =>
        repo.SearchAsync(keyword, currentUserId);

    // ---- Follow ----
    public Task<bool> IsFollowingAsync(int currentUserId, int targetUserId) =>
        repo.IsFollowingAsync(currentUserId, targetUserId);

    public async Task FollowAsync(int currentUserId, int targetUserId)
    {
        if (currentUserId == targetUserId) return;
        await repo.AddFollowAsync(currentUserId, targetUserId);
    }

    public Task UnfollowAsync(int currentUserId, int targetUserId) =>
        repo.RemoveFollowAsync(currentUserId, targetUserId);

    public Task<List<User>> FollowersAsync(int userId) =>
        repo.GetFollowersAsync(userId);

    public Task<List<User>> FollowingAsync(int userId) =>
        repo.GetFollowingAsync(userId);
}