using okem_social.Models;

namespace okem_social.Repositories;

public interface IMessageRepository
{
    Task<List<Message>> GetConversationMessagesAsync(int conversationId, DateTime? before = null, int take = 50);
    Task<Message> CreateAsync(Message message);
    Task<Message?> GetByIdAsync(int id);
}
