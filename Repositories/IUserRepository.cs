using okem_social.Models;

namespace okem_social.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task UpdateAsync(User user);
    Task<List<User>> SearchAsync(string keyword, int excludeUserId, int take = 50);

    // ---- Friend / Kết bạn ----

    // Gửi lời mời kết bạn (fromId -> toId)
    Task SendFriendRequestAsync(int fromId, int toId);

    // Chấp nhận lời mời (fromId đã gửi, toId là người hiện tại)
    Task AcceptFriendRequestAsync(int fromId, int toId);

    // Hủy lời mời mình đã gửi (chưa accept)
    Task CancelFriendRequestAsync(int fromId, int toId);

    // Hủy kết bạn (đã là bạn bè)
    Task RemoveFriendAsync(int userId, int otherId);

    // Kiểm tra đã là bạn bè chưa (2 chiều)
    Task<bool> AreFriendsAsync(int userId, int otherId);

    // Mình đã gửi lời mời & đang chờ nó accept?
    Task<bool> HasPendingRequestAsync(int fromId, int toId);

    // Nó gửi lời mời cho mình & mình đang chờ quyết định?
    Task<bool> HasIncomingRequestAsync(int currentUserId, int otherId);

    // Danh sách bạn bè (2 chiều)
    Task<List<User>> GetFriendsAsync(int userId);

    // Lời mời KẾT BẠN ĐẾN (incoming)
    Task<List<User>> GetIncomingRequestsAsync(int userId);

    // Lời mời KẾT BẠN ĐÃ GỬI (outgoing)
    Task<List<User>> GetOutgoingRequestsAsync(int userId);
}
