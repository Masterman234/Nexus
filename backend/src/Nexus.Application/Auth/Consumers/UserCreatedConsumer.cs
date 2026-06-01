using MassTransit;
using Nexus.Application.Auth.IntegrationEvents;
using Nexus.Application.Abstractions;
using Nexus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Nexus.Application.Auth.Consumers;

public class UserCreatedConsumer(
    IApplicationDbContext dbContext,
    ILogger<UserCreatedConsumer> logger)
    : IConsumer<UserCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedIntegrationEvent> context)
    {
        var @event = context.Message;
        
        logger.LogInformation(
            "Background: Initializing workspace and channels for user {@UserId}", 
            @event.UserId);

        // REAL BUSINESS LOGIC: Create the user's first workspace and a #general channel
        var workspace = Workspace.Create(
            $"{@event.Username}'s Workspace", 
            "Auto-generated personal collaboration hub", 
            @event.UserId);

        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        var channel = Channel.Create("general", "General discussion channel", workspace.Id);
        dbContext.Channels.Add(channel);
        
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Background: Successfully created Workspace {@WorkspaceId} and #general channel", 
            workspace.Id);
    }
}
