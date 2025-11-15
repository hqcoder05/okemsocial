using System.ComponentModel.DataAnnotations;

namespace okem_social.DTOs;

// Auth DTOs
public class RegisterDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(2)]
    public string FullName { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public UserDto User { get; set; } = null!;
}

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = "";
}

// User DTOs
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileDto
{
    [Required, MinLength(2)]
    public string FullName { get; set; } = "";
    public string? AvatarUrl { get; set; }
}

// Post DTOs
public class CreatePostDto
{
    [MaxLength(2000)]
    public string Caption { get; set; } = "";
    
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
}

public class UpdatePostDto
{
    [MaxLength(2000)]
    public string Caption { get; set; } = "";
}

public class PostDto
{
    public int Id { get; set; }
    public UserDto User { get; set; } = null!;
    public string Caption { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Comment DTOs
public class CreateCommentDto
{
    [Required, MaxLength(1000)]
    public string Content { get; set; } = "";
}

public class CommentDto
{
    public int Id { get; set; }
    public UserDto User { get; set; } = null!;
    public string Content { get; set; } = "";
    public int LikesCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Conversation DTOs
public class CreateConversationDto
{
    public List<int> MemberIds { get; set; } = new();
    public string? Name { get; set; }
}

public class UpdateConversationDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";
}

public class ConversationDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsGroup { get; set; }
    public List<UserDto> Members { get; set; } = new();
    public MessageDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Message DTOs
public class MessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public UserDto Sender { get; set; } = null!;
    public string? Content { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    // Flag để frontend dễ phân biệt
    public bool IsMine { get; set; }
}

public class SendMessageDto
{
    public string? Content { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Url { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationDto
{
    public int UserId { get; set; }        // người nhận
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Url { get; set; }
}