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

    public Task<User?> GetByIdAsync(int id) =>
        repo.GetByIdAsync(id);

    public Task<List<User>> SearchAsync(string keyword, int currentUserId) =>
        repo.SearchAsync(keyword, currentUserId);

    // ---- Friend / Kết bạn ----

    public Task<bool> AreFriendsAsync(int currentUserId, int targetUserId) =>
        repo.AreFriendsAsync(currentUserId, targetUserId);

    public Task<bool> HasPendingRequestAsync(int currentUserId, int targetUserId) =>
        repo.HasPendingRequestAsync(currentUserId, targetUserId);

    public Task<bool> HasIncomingRequestAsync(int currentUserId, int targetUserId) =>
        repo.HasIncomingRequestAsync(currentUserId, targetUserId);

    public Task SendFriendRequestAsync(int currentUserId, int targetUserId) =>
        repo.SendFriendRequestAsync(currentUserId, targetUserId);

    public Task AcceptFriendRequestAsync(int fromUserId, int currentUserId) =>
        repo.AcceptFriendRequestAsync(fromUserId, currentUserId);

    public Task CancelFriendRequestAsync(int currentUserId, int targetUserId) =>
        repo.CancelFriendRequestAsync(currentUserId, targetUserId);

    public Task RemoveFriendAsync(int currentUserId, int targetUserId) =>
        repo.RemoveFriendAsync(currentUserId, targetUserId);

    public Task<List<User>> FriendsAsync(int userId) =>
        repo.GetFriendsAsync(userId);

    public Task<List<User>> IncomingRequestsAsync(int currentUserId) =>
        repo.GetIncomingRequestsAsync(currentUserId);

    public Task<List<User>> OutgoingRequestsAsync(int currentUserId) =>
        repo.GetOutgoingRequestsAsync(currentUserId);
}
