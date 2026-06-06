namespace Nexus.Application.Channels;

public record ChannelResponse(Guid Id, string Name, string Description, Guid WorkspaceId, DateTime CreatedAt);
