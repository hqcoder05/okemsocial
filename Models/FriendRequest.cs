using okem_social.Models;

public class FriendRequest
{
    public int FromUserId { get; set; }
    public User? FromUser { get; set; }

    public int ToUserId { get; set; }
    public User? ToUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsAccepted { get; set; } = false;
}
