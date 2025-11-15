using okem_social.Models;

namespace okem_social.Repositories;

public interface IConversationRepository
{
    Task<List<Conversation>> GetUserConversationsAsync(int userId);
    Task<Conversation?> GetByIdAsync(int id, bool includeMembers = false);
    Task<Conversation> CreateAsync(Conversation conversation, List<int> memberIds);
    Task UpdateAsync(Conversation conversation);
    Task AddMemberAsync(int conversationId, int userId);
    Task RemoveMemberAsync(int conversationId, int userId);
    Task<bool> IsMemberAsync(int conversationId, int userId);
    Task<ConversationMember?> GetMemberAsync(int conversationId, int userId);
    Task UpdateLastReadAsync(int conversationId, int userId);
    Task<int> GetUnreadCountAsync(int userId);

    // MỚI: kiếm / tạo đoạn chat 2 người
    Task<Conversation> GetOrCreateDirectConversationAsync(int userId1, int userId2);
}
