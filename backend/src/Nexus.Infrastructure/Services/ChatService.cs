using Microsoft.AspNetCore.SignalR;
using Nexus.Application.Abstractions;
using Nexus.Infrastructure.Realtime;

namespace Nexus.Infrastructure.Services;

public class ChatService(IHubContext<ChatHub> hubContext) : IChatService
{
    public async Task BroadcastMessageAsync(Guid channelId, object message, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(channelId.ToString())
            .SendAsync("ReceiveMessage", message, cancellationToken);
    }

    public async Task BroadcastMessageUpdatedAsync(Guid channelId, object message, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(channelId.ToString())
            .SendAsync("MessageUpdated", message, cancellationToken);
    }

    public async Task BroadcastMessageDeletedAsync(Guid channelId, Guid messageId, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group(channelId.ToString())
            .SendAsync("MessageDeleted", messageId, cancellationToken);
    }
}
