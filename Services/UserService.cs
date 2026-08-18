using okem_social.Models;
using okem_social.Repositories;

namespace okem_social.Services;

public class UserService(IUserRepository repo) : IUserService
{
    public async Task<User> GetMeAsync(int currentUserId) =>
        await repo.GetByIdAsync(currentUserId) ?? throw new KeyNotFoundException("User không tồn tại.");

    public async Task UpdateProfileAsync(int currentUserId, string fullName, string? nickname = null, string? bio = null, string? headline = null, string? location = null, string? websiteUrl = null, string? coverUrl = null, string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("Họ tên không được rỗng.");

        var me = await GetMeAsync(currentUserId);
        me.FullName = fullName.Trim();
        me.Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim();
        me.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        if (headline != null) me.Headline = string.IsNullOrWhiteSpace(headline) ? null : headline.Trim();
        if (location != null) me.Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        if (websiteUrl != null) me.WebsiteUrl = string.IsNullOrWhiteSpace(websiteUrl) ? null : websiteUrl.Trim();
        if (coverUrl != null) me.CoverUrl = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl.Trim();
        if (phoneNumber != null) me.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        me.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(me);
    }

    public async Task UpdateAvatarAsync(int currentUserId, string? avatarUrl)
    {
        var me = await GetMeAsync(currentUserId);
        me.AvatarUrl = avatarUrl;
        me.UpdatedAt = DateTime.UtcNow;
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
