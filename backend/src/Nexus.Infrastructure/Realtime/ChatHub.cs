using Microsoft.AspNetCore.SignalR;

namespace Nexus.Infrastructure.Realtime;

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
