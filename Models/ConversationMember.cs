namespace okem_social.Models;

public class ConversationMember
{
    public int ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReadAt { get; set; }
}
