using System.ComponentModel.DataAnnotations;

namespace okem_social.Models;

public class User
{
    public int Id { get; set; }

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, MaxLength(120)]
    public string FullName { get; set; } = "";

    [MaxLength(50)]
    public string? Nickname { get; set; }

    [MaxLength(300)]
    public string? Bio { get; set; }

    [MaxLength(120)]
    public string? Headline { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    // Ảnh đại diện & Ảnh bìa
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(500)]
    public string? CoverUrl { get; set; }

    public bool PrivateAccount { get; set; } = false;

    [MaxLength(40)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool AutoplayVideoEnabled { get; set; } = true;
    public bool ContentVisibleToPublic { get; set; } = true;
    public bool SearchIndexingEnabled { get; set; } = true;
    public bool TwoFactorEnabled { get; set; } = false;

    public bool Active { get; set; } = true;
    public DateTime? DeactivatedAt { get; set; }

    [Required]                       // lưu hash, KHÔNG lưu plain text
    public string PasswordHash { get; set; } = "";

    public Role Role { get; set; } = Role.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
