using System.ComponentModel.DataAnnotations;

namespace okem_social.Models;

public class Message
{
    public int Id { get; set; }

    [Required]
    public int ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    [Required]
    public int SenderId { get; set; }
    public User? Sender { get; set; }

    [MaxLength(5000)]
    public string? Content { get; set; }

    public string? AttachmentUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}
