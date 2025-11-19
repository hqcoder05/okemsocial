using Microsoft.AspNetCore.SignalR;

namespace okem_social.Hubs;

public class CallHub : Hub
{
    // Dictionary để track các cuộc gọi đang diễn ra
    private static readonly Dictionary<string, CallInfo> ActiveCalls = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var userName = Context.User?.Identity?.Name;
        
        if (!string.IsNullOrEmpty(userId))
        {
            Console.WriteLine($"User {userId} (Name: {userName}) connected to CallHub");
        }
        else
        {
            Console.WriteLine($"WARNING: User connected to CallHub but UserIdentifier is null!");
            Console.WriteLine($"ConnectionId: {Context.ConnectionId}");
            Console.WriteLine($"User authenticated: {Context.User?.Identity?.IsAuthenticated}");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            // Nếu user disconnect mà đang trong cuộc gọi, thông báo cho peer
            if (ActiveCalls.TryGetValue(userId, out var callInfo))
            {
                await Clients.User(callInfo.PeerUserId).SendAsync("CallEnded", new
                {
                    FromUserId = userId,
                    Reason = "disconnected"
                });

                // Xóa cả 2 chiều
                ActiveCalls.Remove(userId);
                ActiveCalls.Remove(callInfo.PeerUserId);
            }

            Console.WriteLine($"User {userId} disconnected from CallHub");
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Bắt đầu cuộc gọi
    public async Task CallUser(string targetUserId, object offer, bool isVideo)
    {
        var callerId = Context.UserIdentifier;

        Console.WriteLine($"CallUser invoked: Caller={callerId}, Target={targetUserId}, IsVideo={isVideo}");

        if (string.IsNullOrEmpty(callerId))
        {
            Console.WriteLine("ERROR: CallUser - callerId is null!");
            await Clients.Caller.SendAsync("CallError", new { message = "Unauthorized" });
            return;
        }

        // Kiểm tra xem target có đang trong cuộc gọi khác không
        if (ActiveCalls.ContainsKey(targetUserId))
        {
            Console.WriteLine($"Target {targetUserId} is busy");
            await Clients.Caller.SendAsync("CallError", new { message = "User is busy" });
            return;
        }

        // Lưu thông tin cuộc gọi
        ActiveCalls[callerId] = new CallInfo
        {
            PeerUserId = targetUserId,
            IsVideo = isVideo,
            StartedAt = DateTime.UtcNow
        };

        // Gửi thông báo cuộc gọi đến
        Console.WriteLine($"Sending IncomingCall to user {targetUserId}");
        await Clients.User(targetUserId).SendAsync("IncomingCall", new
        {
            FromUserId = callerId,
            Offer = offer,
            IsVideo = isVideo
        });

        Console.WriteLine($"Call initiated: {callerId} -> {targetUserId} (video: {isVideo})");
    }

    // Trả lời cuộc gọi
    public async Task AnswerCall(string targetUserId, object answer)
    {
        var calleeId = Context.UserIdentifier;

        if (string.IsNullOrEmpty(calleeId))
        {
            return;
        }

        // Lưu thông tin cuộc gọi cho callee
        if (ActiveCalls.TryGetValue(targetUserId, out var callerInfo))
        {
            ActiveCalls[calleeId] = new CallInfo
            {
                PeerUserId = targetUserId,
                IsVideo = callerInfo.IsVideo,
                StartedAt = DateTime.UtcNow
            };
        }

        await Clients.User(targetUserId).SendAsync("CallAnswered", new
        {
            FromUserId = calleeId,
            Answer = answer
        });

        Console.WriteLine($"Call answered: {calleeId} -> {targetUserId}");
    }

    // Gửi ICE candidate
    public async Task SendIceCandidate(string targetUserId, object candidate)
    {
        var senderId = Context.UserIdentifier;

        if (string.IsNullOrEmpty(senderId))
        {
            return;
        }

        await Clients.User(targetUserId).SendAsync("IceCandidateReceived", new
        {
            FromUserId = senderId,
            Candidate = candidate
        });
    }

    // Kết thúc cuộc gọi
    public async Task HangUp(string targetUserId)
    {
        var userId = Context.UserIdentifier;

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Xóa thông tin cuộc gọi
        ActiveCalls.Remove(userId);
        ActiveCalls.Remove(targetUserId);

        await Clients.User(targetUserId).SendAsync("CallEnded", new
        {
            FromUserId = userId,
            Reason = "hangup"
        });

        Console.WriteLine($"Call ended: {userId} -> {targetUserId}");
    }

    // Từ chối cuộc gọi
    public async Task RejectCall(string targetUserId)
    {
        var userId = Context.UserIdentifier;

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Xóa thông tin cuộc gọi
        ActiveCalls.Remove(userId);
        ActiveCalls.Remove(targetUserId);

        await Clients.User(targetUserId).SendAsync("CallRejected", new
        {
            FromUserId = userId
        });

        Console.WriteLine($"Call rejected: {userId} from {targetUserId}");
    }

    // Toggle video (bật/tắt camera trong cuộc gọi)
    public async Task ToggleVideo(string targetUserId, bool enabled)
    {
        var userId = Context.UserIdentifier;

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await Clients.User(targetUserId).SendAsync("PeerVideoToggled", new
        {
            FromUserId = userId,
            Enabled = enabled
        });
    }

    // Toggle audio (bật/tắt mic trong cuộc gọi)
    public async Task ToggleAudio(string targetUserId, bool enabled)
    {
        var userId = Context.UserIdentifier;

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await Clients.User(targetUserId).SendAsync("PeerAudioToggled", new
        {
            FromUserId = userId,
            Enabled = enabled
        });
    }
}

// Class để lưu thông tin cuộc gọi
public class CallInfo
{
    public string PeerUserId { get; set; } = string.Empty;
    public bool IsVideo { get; set; }
    public DateTime StartedAt { get; set; }
}