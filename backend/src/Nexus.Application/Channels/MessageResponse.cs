namespace Nexus.Application.Channels;

public record MessageResponse(Guid Id, string Content, string Username, Guid ChannelId, DateTime SentAt);
