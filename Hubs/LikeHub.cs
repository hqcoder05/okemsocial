using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace okem_social.Hubs
{
    [Authorize]
    public class LikeHub : Hub
    {
        public static string GetPostGroup(int postId) => $"post-{postId}";

        public async Task JoinPost(int postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetPostGroup(postId));
        }

        public async Task LeavePost(int postId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetPostGroup(postId));
        }
    }
}
