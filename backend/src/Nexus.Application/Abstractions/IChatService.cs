namespace Nexus.Application.Abstractions;

public interface IChatService
{
    Task BroadcastMessageAsync(Guid channelId, object message, CancellationToken cancellationToken = default);
    Task BroadcastMessageUpdatedAsync(Guid channelId, object message, CancellationToken cancellationToken = default);
    Task BroadcastMessageDeletedAsync(Guid channelId, Guid messageId, CancellationToken cancellationToken = default);
}
