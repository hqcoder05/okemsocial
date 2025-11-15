using Microsoft.EntityFrameworkCore;
using okem_social.Data;
using okem_social.Models;

namespace okem_social.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly ApplicationDbContext _db;

    public ConversationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(int userId)
    {
        return await _db.ConversationMembers
            .Where(cm => cm.UserId == userId)
            .Include(cm => cm.Conversation!)
                .ThenInclude(c => c.Members)
                .ThenInclude(m => m.User)
            .Include(cm => cm.Conversation!)
                .ThenInclude(c => c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(1))
                .ThenInclude(m => m.Sender)
            .Select(cm => cm.Conversation!)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Conversation?> GetByIdAsync(int id, bool includeMembers = false)
    {
        var query = _db.Conversations.AsQueryable();

        if (includeMembers)
        {
            query = query
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .Include(c => c.Messages)
                    .ThenInclude(m => m.Sender);
        }

        return await query.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Conversation> CreateAsync(Conversation conversation, List<int> memberIds)
    {
        conversation.CreatedAt = DateTime.UtcNow;
        conversation.UpdatedAt = conversation.CreatedAt;

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        foreach (var memberId in memberIds.Distinct())
        {
            _db.ConversationMembers.Add(new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = memberId,
                JoinedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        // Reload kèm members
        await _db.Entry(conversation)
            .Collection(c => c.Members)
            .Query()
            .Include(m => m.User)
            .LoadAsync();

        return conversation;
    }

    public async Task UpdateAsync(Conversation conversation)
    {
        conversation.UpdatedAt = DateTime.UtcNow;
        _db.Conversations.Update(conversation);
        await _db.SaveChangesAsync();
    }

    public async Task AddMemberAsync(int conversationId, int userId)
    {
        var existing = await _db.ConversationMembers
            .FindAsync(conversationId, userId);

        if (existing == null)
        {
            _db.ConversationMembers.Add(new ConversationMember
            {
                ConversationId = conversationId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
    }

    public async Task RemoveMemberAsync(int conversationId, int userId)
    {
        var membership = await _db.ConversationMembers
            .FindAsync(conversationId, userId);

        if (membership != null)
        {
            _db.ConversationMembers.Remove(membership);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsMemberAsync(int conversationId, int userId)
    {
        return await _db.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
    }

    public async Task<ConversationMember?> GetMemberAsync(int conversationId, int userId)
    {
        return await _db.ConversationMembers
            .Include(cm => cm.Conversation)
            .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
    }

    public async Task UpdateLastReadAsync(int conversationId, int userId)
    {
        var member = await _db.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

        if (member != null)
        {
            member.LastReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        var members = await _db.ConversationMembers
            .Where(cm => cm.UserId == userId)
            .Include(cm => cm.Conversation!)
                .ThenInclude(c => c.Messages)
            .ToListAsync();

        var unreadCount = 0;

        foreach (var member in members)
        {
            var lastRead = member.LastReadAt ?? member.JoinedAt;
            if (lastRead == default)
                lastRead = member.Conversation!.CreatedAt;

            var unreadMessages = member.Conversation!.Messages
                .Count(m => m.CreatedAt > lastRead && m.SenderId != userId);

            unreadCount += unreadMessages;
        }

        return unreadCount;
    }

    // MỚI: tìm hoặc tạo đoạn chat 2 người
    public async Task<Conversation> GetOrCreateDirectConversationAsync(int userId1, int userId2)
    {
        // Luôn sắp xếp cho ổn định
        if (userId1 > userId2)
        {
            (userId1, userId2) = (userId2, userId1);
        }

        // Tìm conversation có đúng 2 member là 2 user này
        var conversation = await _db.Conversations
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .Where(c => c.Members.Count == 2)
            .FirstOrDefaultAsync(c =>
                c.Members.Any(m => m.UserId == userId1) &&
                c.Members.Any(m => m.UserId == userId2));

        if (conversation != null)
            return conversation;

        // Chưa có -> tạo mới
        conversation = new Conversation
        {
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
            // nếu model còn field IsGroup, Title... thì set thêm ở đây
        };

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        _db.ConversationMembers.AddRange(
            new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = userId1,
                JoinedAt = DateTime.UtcNow
            },
            new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = userId2,
                JoinedAt = DateTime.UtcNow
            }
        );

        await _db.SaveChangesAsync();

        await _db.Entry(conversation)
            .Collection(c => c.Members)
            .Query()
            .Include(m => m.User)
            .LoadAsync();

        return conversation;
    }
}
