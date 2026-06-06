using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nexus.Infrastructure.Realtime;

// SignalR honors [Authorize] on the hub class. Browsers can't set Authorization
// headers on WebSocket upgrades, so the JwtBearer pipeline reads the token from
// ?access_token=... for /chatHub (see Api/DependencyInjection.cs).
[Authorize]
public class ChatHub : Hub
{
    public async Task JoinChannel(Guid channelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, channelId.ToString());
    }

    public async Task LeaveChannel(Guid channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId.ToString());
    }
}
