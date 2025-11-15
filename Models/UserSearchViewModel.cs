namespace okem_social.Models;

public class UserSearchViewModel
{
    public User User { get; set; } = null!;

    // Đã là bạn bè 2 chiều hay chưa
    public bool IsFriend { get; set; }

    // Mình đã gửi lời mời cho user này và đang chờ họ chấp nhận
    public bool HasSentRequest { get; set; }

    // User này đã gửi lời mời cho mình và mình đang chờ quyết định
    public bool HasIncomingRequest { get; set; }

    // Có phải chính current user hay không
    public bool IsCurrentUser { get; set; }
}
