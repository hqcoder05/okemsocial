using System.ComponentModel.DataAnnotations;

namespace okem_social.Models;

public class Media
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(500)]
    public string Url { get; set; } = "";

    [MaxLength(50)]
    public string Type { get; set; } = "image"; // image, video

    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
