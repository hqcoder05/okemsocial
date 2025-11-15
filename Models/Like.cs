namespace okem_social.Models;

public class Like
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    // Like có thể thuộc Post hoặc Comment
    public int? PostId { get; set; }
    public Post? Post { get; set; }

    public int? CommentId { get; set; }
    public Comment? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
