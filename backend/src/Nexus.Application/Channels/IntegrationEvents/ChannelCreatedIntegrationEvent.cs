namespace Nexus.Application.Channels.IntegrationEvents;

public record ChannelCreatedIntegrationEvent(
    Guid ChannelId,
    string Name,
    string Description,
    Guid WorkspaceId,
    DateTime CreatedAt);
