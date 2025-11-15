using System.ComponentModel.DataAnnotations;

namespace okem_social.Models;

public class User
{
    public int Id { get; set; }

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, MaxLength(120)]
    public string FullName { get; set; } = "";

    // Ảnh đại diện (có thể null)
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [Required]                       // lưu hash, KHÔNG lưu plain text
    public string PasswordHash { get; set; } = "";

    public Role Role { get; set; } = Role.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
