namespace Nexus.Domain.Entities;

/// <summary>
/// Well-known deterministic identifiers for system (bot) users. Stable across
/// environments so consumers can safely reference them as a FK without a lookup.
/// </summary>
public static class SystemUsers
{
    public static readonly Guid GithubBotId = new("11111111-1111-1111-1111-111111111111");
    public const string GithubBotUsername = "github-bot";
    public const string GithubBotEmail = "github-bot@nexus.system";

    // NEX-18: separate identity for AI / slash-command replies so users can tell
    // "code activity from GitHub" apart from "AI assistant" at a glance in the UI.
    public static readonly Guid NexusBotId = new("22222222-2222-2222-2222-222222222222");
    public const string NexusBotUsername = "nexus-bot";
    public const string NexusBotEmail = "nexus-bot@nexus.system";
}
