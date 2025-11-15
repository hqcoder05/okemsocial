using okem_social.Models;

namespace okem_social.Services;

public interface IUserService
{
    Task<User> GetMeAsync(int currentUserId);
    Task UpdateProfileAsync(int currentUserId, string fullName);
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> SearchAsync(string keyword, int currentUserId);

    // ---- Friend / Kết bạn ----
    Task<bool> AreFriendsAsync(int currentUserId, int targetUserId);
    Task<bool> HasPendingRequestAsync(int currentUserId, int targetUserId);
    Task<bool> HasIncomingRequestAsync(int currentUserId, int targetUserId);

    Task SendFriendRequestAsync(int currentUserId, int targetUserId);
    Task AcceptFriendRequestAsync(int fromUserId, int currentUserId);
    Task CancelFriendRequestAsync(int currentUserId, int targetUserId);
    Task RemoveFriendAsync(int currentUserId, int targetUserId);

    Task<List<User>> FriendsAsync(int userId);

    // Lời mời kết bạn đến (người khác gửi cho mình)
    Task<List<User>> IncomingRequestsAsync(int currentUserId);

    // Lời mời kết bạn đã gửi (mình gửi cho người khác)
    Task<List<User>> OutgoingRequestsAsync(int currentUserId);
}
