using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace okem_social.Hubs;

[Authorize]
public class CommentHub : Hub
{
    public Task JoinPost(int postId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GetPostGroup(postId));

    public Task LeavePost(int postId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GetPostGroup(postId));

    public static string GetPostGroup(int postId) => $"post-{postId}";
}
