using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Nexus.Infrastructure.Realtime;

namespace Nexus.Api.Realtime;

public class ChannelCreatedHubConsumer(IHubContext<ChatHub> hubContext) 
    : IConsumer<Application.Channels.IntegrationEvents.ChannelCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<Application.Channels.IntegrationEvents.ChannelCreatedIntegrationEvent> context)
    {
        var @event = context.Message;
        
        // Broadcast to all connected clients that a new channel was created
        await hubContext.Clients.All.SendAsync("ChannelCreated", new 
        {
            Id = @event.ChannelId,
            Name = @event.Name,
            Description = @event.Description,
            WorkspaceId = @event.WorkspaceId
        });
    }
}
