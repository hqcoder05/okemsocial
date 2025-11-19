using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace okem_social.Hubs;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Lấy UserId từ ClaimTypes.NameIdentifier
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
