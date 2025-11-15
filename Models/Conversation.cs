using System.ComponentModel.DataAnnotations;

namespace okem_social.Models;

public class Conversation
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string? Name { get; set; } // Tên group chat (null nếu 1-1)

    public bool IsGroup { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
