namespace Nexus.Application.ChatCommands;

/// <summary>
/// Routes a parsed slash command to its handler and broadcasts the response
/// back into the originating channel as a bot message. Called from
/// <c>SendMessage.Handler</c> only when the message starts with <c>/</c>;
/// non-command messages bypass this entirely.
/// <para>
/// Implementations should run the dispatch <b>off the calling request</b> so
/// the original <c>POST /messages</c> returns immediately — a multi-second AI
/// call should never block the user's HTTP round-trip. The bot reply lands
/// via SignalR a few seconds later, matching the Slack / Discord pattern.
/// </para>
/// </summary>
public interface IChatCommandRouter
{
    /// <summary>
    /// Schedule the command for background dispatch. Returns immediately —
    /// the actual work runs in a fresh DI scope so it can outlive the calling
    /// HTTP request without depending on its scoped services.
    /// </summary>
    void Schedule(ChatCommand command, Guid invokingUserId, Guid channelId);
}
