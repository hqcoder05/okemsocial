using System.ComponentModel.DataAnnotations;

namespace okem_social.Models;

public class Comment
{
    public int Id { get; set; }

    [Required]
    public int PostId { get; set; }
    public Post? Post { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(1000)]
    public string Content { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
