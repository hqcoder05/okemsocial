using Microsoft.EntityFrameworkCore;
using okem_social.Data;
using okem_social.Models;

namespace okem_social.Repositories;

public class MessageRepository(ApplicationDbContext db) : IMessageRepository
{
    public async Task<List<Message>> GetConversationMessagesAsync(int conversationId, DateTime? before = null, int take = 50)
    {
        var query = db.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted);

        if (before.HasValue)
        {
            query = query.Where(m => m.CreatedAt < before.Value);
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .OrderBy(m => m.CreatedAt) // Đảo ngược lại cho đúng thứ tự
            .ToListAsync();
    }

    public async Task<Message> CreateAsync(Message message)
    {
        db.Messages.Add(message);
        await db.SaveChangesAsync();

        // Update conversation UpdatedAt
        var conversation = await db.Conversations.FindAsync(message.ConversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        // Reload with sender
        await db.Entry(message).Reference(m => m.Sender).LoadAsync();
        return message;
    }

    public async Task<Message?> GetByIdAsync(int id)
    {
        return await db.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == id);
    }
}
