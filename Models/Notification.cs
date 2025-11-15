namespace okem_social.Models
{
    public class Notification
    {
        public int Id { get; set; }

        /// <summary>Người nhận thông báo</summary>
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>Loại thông báo: FriendRequest, Message, System,...</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Tiêu đề ngắn</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Nội dung mô tả</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>Url để redirect khi user click vào thông báo (optional)</summary>
        public string? Url { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
